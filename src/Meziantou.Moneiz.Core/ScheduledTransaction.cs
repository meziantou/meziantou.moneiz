using System.Text.Json.Serialization;
using Meziantou.Framework.Scheduling;

namespace Meziantou.Moneiz.Core;

public sealed class ScheduledTransaction
{
    private int? _accountId;
    private int? _creditedAccountId;
    private int? _payeeId;
    private int? _categoryId;

    [JsonPropertyName("a")]
    public int Id { get; set; }

    [JsonPropertyName("b")]
    public string? Name { get; set; }

    [JsonPropertyName("c")]
    public string? RecurrenceRuleText { get; set; }

    [JsonPropertyName("d")]
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("e")]
    public decimal Amount { get; set; }

    [JsonPropertyName("f")]
    public string? Comment { get; set; }

    [JsonIgnore]
    public Account? Account
    {
        get;
        set
        {
            field = value;
            _accountId = null;
        }
    }

    [JsonPropertyName("g")]
    public int? AccountId
    {
        get => Account?.Id ?? _accountId;
        set => _accountId = value;
    }

    [JsonIgnore]
    public Account? CreditedAccount
    {
        get;
        set
        {
            field = value;
            _creditedAccountId = null;
        }
    }

    [JsonPropertyName("h")]
    public int? CreditedAccountId
    {
        get => CreditedAccount?.Id ?? _creditedAccountId;
        set => _creditedAccountId = value;
    }

    [JsonIgnore]
    public Payee? Payee
    {
        get;
        set
        {
            field = value;
            _payeeId = null;
        }
    }

    [JsonPropertyName("i")]
    public int? PayeeId
    {
        get => Payee?.Id ?? _payeeId;
        set => _payeeId = value;
    }

    [JsonIgnore]
    public Category? Category
    {
        get;
        set
        {
            field = value;
            _categoryId = null;
        }
    }

    [JsonPropertyName("j")]
    public int? CategoryId
    {
        get => Category?.Id ?? _categoryId;
        set => _categoryId = value;
    }

    [JsonPropertyName("k")]
    [JsonConverter(typeof(NullableDateOnlyJsonConverter))]
    public DateOnly? NextOccurenceDate { get; set; }

    [JsonPropertyName("l")]
    public string[]? Labels { get; set; }

    [JsonIgnore]
    public RecurrenceRule? RecurrenceRule
    {
        get
        {
            if (RecurrenceRule.TryParse(RecurrenceRuleText, out var recurrenceRule))
                return recurrenceRule;

            return null;
        }
    }

    public IEnumerable<DateTime> GetNextOccurences()
    {
        if (NextOccurenceDate is null)
            return [];

        return GetOccurrences(NextOccurenceDate.Value).Select(date => date.ToDateTime(TimeOnly.MinValue));
    }

    /// <summary>
    /// Gets the occurrences of the schedule that are on or after <paramref name="minDate"/>.
    /// </summary>
    /// <remarks>
    /// The occurrences are always computed from <see cref="StartDate"/> as the recurrence rule is anchored on it.
    /// For instance, <c>FREQ=MONTHLY</c> repeats on the day of the start date, and <c>INTERVAL</c> and <c>COUNT</c> are relative to it.
    /// </remarks>
    internal IEnumerable<DateOnly> GetOccurrences(DateOnly minDate)
    {
        var recurrenceRule = RecurrenceRule;
        if (recurrenceRule is null)
            yield break;

        DateOnly? previousOccurrence = null;
        foreach (var occurrence in recurrenceRule.GetNextOccurrences(StartDate.ToDateTime(TimeOnly.MinValue)))
        {
            var occurrenceDate = DateOnly.FromDateTime(occurrence);

            // The recurrence rule must move forward, otherwise the enumeration never ends
            if (previousOccurrence >= occurrenceDate)
                yield break;

            previousOccurrence = occurrenceDate;
            if (occurrenceDate >= minDate)
                yield return occurrenceDate;
        }
    }

    internal void ResolveReferences(DatabaseReferenceIndex index)
    {
        if (_accountId.HasValue)
        {
            Account = index.Accounts.GetById(_accountId);
        }

        if (_creditedAccountId.HasValue)
        {
            CreditedAccount = index.Accounts.GetById(_creditedAccountId);
        }

        if (_payeeId.HasValue)
        {
            Payee = index.Payees.GetById(_payeeId);
        }

        if (_categoryId.HasValue)
        {
            Category = index.Categories.GetById(_categoryId);
        }
    }
}
