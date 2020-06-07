using System;
using System.Linq;

namespace Meziantou.Moneiz.Core
{
    partial class Database
    {
        public Transaction? GetTransactionById(int? id)
        {
            if (id == null)
                return null;

            return Transactions.FirstOrDefault(item => item.Id == id);
        }


        public void SaveTransaction(Transaction transaction)
        {
            var existingTransaction = Transactions.FirstOrDefault(item => item.Id == transaction.Id);
            if (existingTransaction == null)
            {
                transaction.Id = GenerateId(Payees, item => item.Id);
            }

            AddOrReplace(Transactions, existingTransaction, transaction);
            RaiseDatabaseChanged();
        }

        public void RemoveTransaction(Transaction transaction)
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

        public void CheckTransaction(Transaction transaction)
        {
            if (transaction.CheckedDate == null)
            {
                transaction.CheckedDate = DateTime.UtcNow;
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
