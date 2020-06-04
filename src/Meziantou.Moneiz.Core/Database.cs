using System;
using System.Collections.Generic;
using System.Linq;
using Meziantou.Framework;

namespace Meziantou.Moneiz.Core
{
    public sealed class Database
    {
        public event EventHandler? DatabaseChanged;

        public IList<Account> Accounts { get; set; } = new List<Account>();
        public IList<Currency> Currencies { get; set; } = new List<Currency>();
        public IList<CategoryGroup> CategoryGroups { get; set; } = new List<CategoryGroup>();

        public Account? GetAccountById(int? id)
        {
            if (id == null)
                return null;

            return Accounts.FirstOrDefault(account => account.Id == id);
        }

        public void SaveAccount(Account account)
        {
            var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);
            if (existingAccount == null)
            {
                account.Id = GenerateId(Accounts, a => a.Id);
                Accounts.Add(account);
            }
            else
            {
                Accounts.AddOrReplace(existingAccount, account);
            }

            RaiseDatabaseChanged();
        }

        public void MoveUpAccount(Account account)
        {
            MoveAccount(account, -1);
        }

        public void MoveDownAccount(Account account)
        {
            MoveAccount(account, 1);
        }

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
                RaiseDatabaseChanged();
            }
        }

        private static int GenerateId<T>(IEnumerable<T> items, Func<T, int> idSelector)
        {
            var max = 0;
            foreach (var item in items)
            {
                var id = idSelector(item);
                if (id > max)
                {
                    max = id;
                }
            }

            return max + 1;
        }

        private void RaiseDatabaseChanged()
        {
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
