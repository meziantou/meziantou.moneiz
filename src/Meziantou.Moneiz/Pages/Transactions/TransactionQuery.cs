using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Meziantou.Framework;
using Meziantou.Framework.SimpleQueryLanguage;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Extensions;

namespace Meziantou.Moneiz.Pages.Transactions;

internal static class TransactionQueries
{
    public static QueryBuilder<Transaction> TransactionQuery { get; } = CreateTransactionQuery();

    private static QueryBuilder<Transaction> CreateTransactionQuery()
    {
        static bool MatchStringValue(string value, string query)
        {
            if (value is null)
                return false;

            value = value.RemoveDiacritics();
            query = query.RemoveDiacritics();
            return value.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        var builder = new QueryBuilder<Transaction>();
        builder.AddHandler("bank", (transaction, text) => MatchStringValue(transaction.Account?.Bank, text));
        builder.AddHandler("account", (transaction, text) => MatchStringValue(transaction.Account?.Name, text));
        builder.AddHandler<int>("accountId", (transaction, id) => transaction.Account?.Id == id);
        builder.AddHandler("title", (transaction, text) => MatchStringValue(transaction.FinalTitle, text));
        builder.AddHandler("payee", (transaction, text) => MatchStringValue(transaction.Payee?.Name, text));
        builder.AddHandler("category", (transaction, text) => MatchStringValue(transaction.Category?.Name, text));
        builder.AddHandler("categoryGroup", (transaction, text) => MatchStringValue(transaction.Category?.GroupName, text));
        builder.AddHandler("comment", (transaction, text) => MatchStringValue(transaction.Comment, text));
        builder.AddHandler<TransactionState>("state", (transaction, value) => transaction.State == value);
        builder.AddRangeHandler<DateOnly>("date", (transaction, range) => range.IsInRange(transaction.ValueDate));
        builder.AddRangeHandler<decimal>("amount", (transaction, range) => range.IsInRange(transaction.Amount));

        builder.SetTextFilterHandler((transaction, text) =>
        {
            var amount = ExtractDecimalValues(text)?.LastOrDefault();
            if (MatchStringValue(transaction.FinalTitle, text))
                return true;

            if (MatchStringValue(transaction.Payee?.Name, text))
                return true;

            if (MatchStringValue(transaction.Category?.Name, text))
                return true;

            if (MatchStringValue(transaction.Category?.GroupName, text))
                return true;

            if (MatchStringValue(transaction.Comment, text))
                return true;

            if (amount.HasValue)
            {
                if (transaction.Amount == amount)
                    return true;
            }

            return false;
        });

        return builder;
    }

    private static IEnumerable<decimal> ExtractDecimalValues(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var part in parts)
        {
            // Ensure the value is formatted as it is displayed (12.00 instead of 12)
            // Use same format as in Amount.razor
            if (decimal.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && part == AmountUtilities.FormatAmount(result))
                yield return result;
        }
    }
}
