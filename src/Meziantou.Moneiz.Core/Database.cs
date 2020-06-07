using System;
using System.Collections.Generic;

namespace Meziantou.Moneiz.Core
{
    public sealed partial class Database
    {
        public event EventHandler? DatabaseChanged;

        public IList<Account> Accounts { get; set; } = new List<Account>();
        public IList<Currency> Currencies { get; set; } = new List<Currency>();
        public IList<Category> Categories { get; set; } = new List<Category>();
        public IList<Payee> Payees { get; set; } = new List<Payee>();
        public IList<Transaction> Transactions { get; set; } = new List<Transaction>();

        private static void AddOrReplace<T>(IList<T> items, T? existingItem, T newItem) where T : class
        {
            if (existingItem != null)
            {
                var index = items.IndexOf(existingItem);
                if (index >= 0)
                {
                    items[index] = newItem;
                    return;
                }
            }

            items.Add(newItem);
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
