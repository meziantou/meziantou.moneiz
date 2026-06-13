using Meziantou.Framework;

namespace Meziantou.Moneiz.Core;

public partial class Database
{
    public Payee? GetPayeeById(int? id)
    {
        if (id is null)
            return null;

        return Payees.FirstOrDefault(item => item.Id == id);
    }

    public Payee? GetPayeeByName(string? name)
    {
        if (name is null)
            return null;

        return Payees.FirstOrDefault(item => item.Name == name);
    }

    public Payee? GetOrCreatePayeeByName(string? name)
    {
        if (name is null)
            return null;

        var payee = Payees.FirstOrDefault(item => item.Name == name);
        if (payee is null)
        {
            payee = new Payee { Name = name };
            SavePayee(payee);
        }

        return payee;
    }

    public IReadOnlyList<Payee> GetPayeeSuggestions(Account? account, string? query, int maximumCount)
    {
        if (maximumCount <= 0)
            return [];

        var usage = Transactions
            .Where(transaction => account is not null && transaction.Payee is not null && transaction.Account == account)
            .GroupBy(transaction => transaction.Payee!)
            .ToDictionary(
                group => group.Key,
                group => new PayeeUsage(group.Count(), group.Max(transaction => transaction.ValueDate)));

        var normalizedQuery = NormalizeForSearch(query);
        if (normalizedQuery.Length == 0)
        {
            return Payees
                .OrderByDescending(payee => usage.GetValueOrDefault(payee).Count)
                .ThenByDescending(payee => usage.GetValueOrDefault(payee).LatestTransactionDate)
                .ThenBy(payee => payee.Name, StringComparer.OrdinalIgnoreCase)
                .Take(maximumCount)
                .ToList();
        }

        var queryTerms = normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return Payees
            .Select(payee => new PayeeSuggestion(payee, GetMatchRank(NormalizeForSearch(payee.Name), normalizedQuery, queryTerms), usage.GetValueOrDefault(payee)))
            .Where(suggestion => suggestion.Match.Rank != int.MaxValue)
            .OrderBy(suggestion => suggestion.Match.Rank)
            .ThenBy(suggestion => suggestion.Match.FuzzyDistance)
            .ThenByDescending(suggestion => suggestion.Usage.Count)
            .ThenByDescending(suggestion => suggestion.Usage.LatestTransactionDate)
            .ThenBy(suggestion => suggestion.Payee.Name, StringComparer.OrdinalIgnoreCase)
            .Take(maximumCount)
            .Select(suggestion => suggestion.Payee)
            .ToList();
    }

    public void RemovePayee(Payee payee)
    {
        using (DeferEvents())
        {
            ReplacePayee(oldPayee: payee, newPayee: null);
            _ = Payees.Remove(payee);
            RaiseDatabaseChanged();
        }
    }

    public void SavePayee(Payee payee)
    {
        using (DeferEvents())
        {
            var existingPayee = Payees.FirstOrDefault(item => item.Id == payee.Id);
            if (existingPayee is null)
            {
                payee.Id = GenerateId(Payees, item => item.Id);
            }

            AddOrReplace(Payees, existingPayee, payee);
            MergePayees();
            RaiseDatabaseChanged();
        }
    }

    private void MergePayees()
    {
        foreach (var group in Payees.GroupBy(c => c.Name, StringComparer.Ordinal))
        {
            var first = group.First();
            foreach (var item in group.Skip(1))
            {
                ReplacePayee(oldPayee: item, newPayee: first);
                RemovePayee(item);
            }
        }
    }

    private void ReplacePayee(Payee? oldPayee, Payee? newPayee)
    {
        ReplacePayee(newPayee, p => p == oldPayee);
    }

    private void ReplacePayee(Payee? newPayee, Func<Payee?, bool> predicate)
    {
        foreach (var scheduledTransactions in ScheduledTransactions)
        {
            if (predicate(scheduledTransactions.Payee))
            {
                scheduledTransactions.Payee = newPayee;
            }
        }

        foreach (var transactions in Transactions)
        {
            if (predicate(transactions.Payee))
            {
                transactions.Payee = newPayee;
            }
        }
    }

    private static PayeeMatch GetMatchRank(string payeeName, string normalizedQuery, string[] queryTerms)
    {
        if (payeeName.Equals(normalizedQuery, StringComparison.Ordinal))
            return new PayeeMatch(Rank: 0, FuzzyDistance: 0);

        if (payeeName.StartsWith(normalizedQuery, StringComparison.Ordinal))
            return new PayeeMatch(Rank: 1, FuzzyDistance: 0);

        if (queryTerms.Length == 1 && payeeName.Contains(normalizedQuery, StringComparison.Ordinal))
            return new PayeeMatch(Rank: 2, FuzzyDistance: 0);

        if (queryTerms.All(term => payeeName.Contains(term, StringComparison.Ordinal)))
            return new PayeeMatch(Rank: 3, FuzzyDistance: 0);

        var payeeTerms = payeeName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var totalDistance = 0;
        foreach (var queryTerm in queryTerms)
        {
            var maximumDistance = queryTerm.Length switch
            {
                < 3 => 0,
                <= 5 => 1,
                _ => 2,
            };

            if (maximumDistance == 0)
                return PayeeMatch.NoMatch;

            var distance = payeeTerms
                .Select(term => GetEditDistance(queryTerm, term, maximumDistance))
                .Min();
            if (distance > maximumDistance)
                return PayeeMatch.NoMatch;

            totalDistance += distance;
        }

        return new PayeeMatch(Rank: 4, FuzzyDistance: totalDistance);
    }

    private static int GetEditDistance(string left, string right, int maximumDistance)
    {
        if (Math.Abs(left.Length - right.Length) > maximumDistance)
            return maximumDistance + 1;

        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var index = 0; index <= right.Length; index++)
        {
            previous[index] = index;
        }

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            var rowMinimum = current[0];
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var substitutionCost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + substitutionCost);
                rowMinimum = Math.Min(rowMinimum, current[rightIndex]);
            }

            if (rowMinimum > maximumDistance)
                return maximumDistance + 1;

            var temporary = previous;
            previous = current;
            current = temporary;
        }

        return previous[right.Length];
    }

    private static string NormalizeForSearch(string? value)
    {
        return value?.RemoveDiacritics().Trim().ToUpperInvariant() ?? "";
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    private readonly record struct PayeeUsage(int Count, DateOnly LatestTransactionDate);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    private readonly record struct PayeeMatch(int Rank, int FuzzyDistance)
    {
        public static PayeeMatch NoMatch { get; } = new(Rank: int.MaxValue, FuzzyDistance: int.MaxValue);
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto)]
    private readonly record struct PayeeSuggestion(Payee Payee, PayeeMatch Match, PayeeUsage Usage);
}
