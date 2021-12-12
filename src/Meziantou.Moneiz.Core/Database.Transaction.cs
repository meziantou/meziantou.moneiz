using System.Linq;

namespace Meziantou.Moneiz.Core
{
    public partial class Database
    {
        public Transaction? GetTransactionById(int? id)
        {
            if (id == null)
                return null;

            return Transactions.FirstOrDefault(item => item.Id == id);
        }

        public Transaction? GetDebitedTransactionById(int? id)
        {
            if (id == null)
                return null;

            var transaction = Transactions.FirstOrDefault(item => item.Id == id);
            if (transaction == null)
                return null;

            if (transaction.Amount > 0 && transaction.LinkedTransaction != null)
                return transaction.LinkedTransaction;

            return transaction;
        }

        public void SaveTransaction(Transaction transaction)
        {
            using (DeferEvents())
            {
                var existingTransaction = Transactions.FirstOrDefault(item => item.Id == transaction.Id);
                if (existingTransaction == null)
                {
                    transaction.Id = GenerateId(Transactions, item => item.Id);
                }

                AddOrReplace(Transactions, existingTransaction, transaction);

                if (transaction.Payee != null && transaction.Payee.DefaultCategory == null && transaction.Category != null)
                {
                    transaction.Payee.DefaultCategory = transaction.Category;
                    SavePayee(transaction.Payee);
                }

                RaiseDatabaseChanged();
            }
        }

        public void RemoveTransaction(Transaction transaction)
        {
            using (DeferEvents())
            {
                foreach (var t in Transactions.Where(t => t.LinkedTransaction == transaction))
                {
                    t.LinkedTransaction = null;
                }

                if (Transactions.Remove(transaction))
                {
                    RaiseDatabaseChanged();
                }
            }
        }

        public void CheckTransaction(Transaction transaction)
        {
            if (transaction.CheckedDate == null)
            {
                transaction.CheckedDate = GetToday();
                RaiseDatabaseChanged();
            }
        }

        public void UncheckTransaction(Transaction transaction)
        {
            if (transaction.CheckedDate != null)
            {
                transaction.CheckedDate = null;
                RaiseDatabaseChanged();
            }
        }
    }
}
