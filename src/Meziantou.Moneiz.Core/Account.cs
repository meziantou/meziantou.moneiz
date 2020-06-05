namespace Meziantou.Moneiz.Core
{
    public sealed class Account
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? CurrencyIsoCode { get; set; }
        public CreditDebit DefaultCreditDebit { get; set; }
        public TransactionState DefaultTransactionState { get; set; }
        public TransactionType DefaultTransactionType { get; set; }
        public AccountType AccountType { get; set; }
        public decimal InitialBalance { get; set; }
        public bool ShowOnSidebar { get; set; }
        public int SortOrder { get; set; }
        public string? HolderName { get; set; }
        public string? Bank { get; set; }

        public decimal Balance => InitialBalance;

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
    }
}
