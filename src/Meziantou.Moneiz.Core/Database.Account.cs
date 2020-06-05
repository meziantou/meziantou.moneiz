using System.Linq;

namespace Meziantou.Moneiz.Core
{
    partial class Database
    {
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
    }
}
