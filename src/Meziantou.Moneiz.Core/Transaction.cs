using System;
using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    public sealed class Transaction
    {
        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public decimal Amount { get; set; }

        [JsonPropertyName("c")]
        public string? Comment { get; set; }

        [JsonPropertyName("d")]
        public DateTime ValueDate { get; set; }

        [JsonPropertyName("e")]
        public DateTime? CheckedDate { get; set; }

        [JsonPropertyName("f")]
        public DateTime? ReconciliationDate { get; set; }

        [JsonPropertyName("g")]
        public Account? Account { get; set; }

        [JsonPropertyName("h")]
        public Payee? Payee { get; set; }

        [JsonPropertyName("i")]
        public Category? Category { get; set; }

        [JsonPropertyName("j")]
        public Transaction? LinkedTransaction { get; set; }

        [JsonIgnore]
        public string? FinalTitle => Payee?.ToString() ?? LinkedTransaction?.Account?.ToString();

        [JsonIgnore]
        public TransactionState State
        {
            get
            {
                if (ReconciliationDate.HasValue)
                    return TransactionState.Reconciliated;

                if (CheckedDate.HasValue)
                    return TransactionState.Checked;

                return TransactionState.NotChecked;
            }
        }
    }
}
