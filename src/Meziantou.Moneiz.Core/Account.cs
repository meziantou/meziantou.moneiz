using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    public sealed class Account
    {
        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        [JsonPropertyName("c")]
        public string? CurrencyIsoCode { get; set; }

        [JsonPropertyName("d")]
        public CashFlow DefaultCashFlow { get; set; }

        [JsonPropertyName("e")]
        public TransactionState DefaultTransactionState { get; set; }

        [JsonPropertyName("f")]
        public AccountType AccountType { get; set; }

        [JsonPropertyName("g")]
        public decimal InitialBalance { get; set; }

        [JsonPropertyName("h")]
        public bool Closed { get; set; }

        [JsonPropertyName("i")]
        public int SortOrder { get; set; }

        [JsonPropertyName("j")]
        public string? HolderName { get; set; }

        [JsonPropertyName("k")]
        public string? Bank { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Bank))
            {
                return Bank + " - " + Name;
            }
            else
            {
                return Name ?? "";
            }
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object? obj)
        {
            return obj is Account a && a.Id == Id;
        }
    }
}
