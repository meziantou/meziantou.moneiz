using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    partial class Database
    {
        [JsonIgnore]
        public Account? DefaultAccount => Accounts.Where(a => a.ShowOnSidebar).OrderBy(a => a.ToString()).FirstOrDefault();

        public Account? GetAccountById(int? id)
        {
            if (id == null)
                return null;

            return Accounts.FirstOrDefault(item => item.Id == id);
        }

        public void SaveAccount(Account account)
        {
            var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);
            if (existingAccount == null)
            {
                account.Id = GenerateId(Accounts, a => a.Id);
            }

            AddOrReplace(Accounts, existingAccount, account);
            RaiseDatabaseChanged();
        }

        public void MoveUpAccount(Account account) => MoveAccount(account, -1);

        public void MoveDownAccount(Account account) => MoveAccount(account, 1);

        private void MoveAccount(Account account, int direction)
        {
            var accounts = Accounts.OrderBy(a => a.SortOrder).ToList();
            for (var i = 0; i < accounts.Count; i++)
            {
                accounts[i].SortOrder = i;
            }

            var newSortOrder = account.SortOrder + direction;
            if (newSortOrder >= 0 && newSortOrder < accounts.Count)
            {
                accounts.First(a => a.SortOrder == newSortOrder).SortOrder = account.SortOrder;
                account.SortOrder = newSortOrder;

                RaiseDatabaseChanged();
            }
        }

        public void RemoveAccount(Account account)
        {
            if (Accounts.Remove(account))
            {
                // TODO remove transactions, etc.
                RaiseDatabaseChanged();
            }
        }

        public decimal GetReconciledBalance(Account account)
        {
            return GetBalance(account, DateTime.MaxValue, TransactionState.Reconciliated);
        }

        public decimal GetCheckedBalance(Account account)
        {
            return GetBalance(account, DateTime.MaxValue, TransactionState.Checked);
        }

        public decimal GetTodayBalance(Account account)
        {
            return GetBalance(account, DateTime.UtcNow, TransactionState.NotChecked);
        }

        public decimal GetBalance(Account account)
        {
            return GetBalance(account, DateTime.MaxValue, TransactionState.NotChecked);
        }

        private decimal GetBalance(Account account, DateTime dateTime, TransactionState transactionState)
        {
            return account.InitialBalance + Transactions.Where(IncludeTransaction).Sum(t => t.Amount);

            bool IncludeTransaction(Transaction transaction)
            {
                if (transaction.Account != account)
                    return false;

                if (transaction.ValueDate > dateTime)
                    return false;

                if (transactionState == TransactionState.Reconciliated && transaction.ReconciliationDate == null)
                    return false;

                if (transactionState == TransactionState.Checked && transaction.CheckedDate == null)
                    return false;

                return true;
            }
        }

        public void Reconcile(Account account)
        {
            var now = DateTime.UtcNow;
            foreach (var transaction in Transactions.Where(t => t.Account == account && t.State == TransactionState.Checked))
            {
                transaction.ReconciliationDate = now;
            }
        }
    }
}
