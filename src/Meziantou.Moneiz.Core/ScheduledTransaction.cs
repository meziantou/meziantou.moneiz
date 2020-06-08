using System;
using System.Text.Json.Serialization;
using Meziantou.Framework.Scheduling;

namespace Meziantou.Moneiz.Core
{
    public sealed class ScheduledTransaction
    {
        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        [JsonPropertyName("c")]
        public string? RecurrenceRuleText { get; set; }

        [JsonPropertyName("d")]
        public DateTime StartDate { get; set; }

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
        public DateTime? NextOccurenceDate { get; set; }

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
    }
}
