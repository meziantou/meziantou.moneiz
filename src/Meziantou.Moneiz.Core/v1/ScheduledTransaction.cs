using System;
using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core.V1;

internal sealed class ScheduledTransaction
{
    [JsonPropertyName("a")]
    public int Id { get; set; }

    [JsonPropertyName("b")]
    public string? Name { get; set; }

    [JsonPropertyName("c")]
    public string? RecurrenceRuleText { get; set; }

    [JsonPropertyName("d")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("e")]
    public decimal Amount { get; set; }

    [JsonPropertyName("f")]
    public string? Comment { get; set; }

    [JsonPropertyName("g")]
    public Account? Account { get; set; }

    [JsonPropertyName("h")]
    public Account? CreditedAccount { get; set; }

    [JsonPropertyName("i")]
    public Payee? Payee { get; set; }

    [JsonPropertyName("j")]
    public Category? Category { get; set; }

    [JsonPropertyName("k")]
    public DateOnly? NextOccurenceDate { get; set; }
}
