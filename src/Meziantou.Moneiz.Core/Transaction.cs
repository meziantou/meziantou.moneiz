using System;

namespace Meziantou.Moneiz.Core
{
    public sealed class Transaction
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }
        public string? Comment { get; set; }
        public DateTime ValueDate { get; set; }
        public DateTime? CheckedDate { get; set; }
        public DateTime? ReconciliationDate { get; set; }

        public Account? Account { get; set; }
        public Payee? Payee { get; set; }
        public Category? Category { get; set; }
        public Transaction? LinkedTransaction { get; set; }

        //public ScheduledTransaction ScheduledTransaction { get; set; }

        public string? FinalTitle => Payee?.ToString() ?? LinkedTransaction?.Account?.ToString();

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
