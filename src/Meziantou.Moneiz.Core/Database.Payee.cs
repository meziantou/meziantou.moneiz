using System;
using System.Linq;

namespace Meziantou.Moneiz.Core
{
    partial class Database
    {
        public Payee? GetPayeeById(int? id)
        {
            if (id == null)
                return null;

            return Payees.FirstOrDefault(item => item.Id == id);
        }

        public void RemovePayee(Payee payee)
        {
            ReplacePayee(oldPayee: payee, newPayee: null);
            if (Payees.Remove(payee))
            {
                RaiseDatabaseChanged();
            }
        }

        public void SavePayee(Payee payee)
        {
            var existingPayee = Payees.FirstOrDefault(item => item.Id == payee.Id);
            if (existingPayee == null)
            {
                payee.Id = GenerateId(Payees, item => item.Id);
            }

            AddOrReplace(Payees, existingPayee, payee);
            MergePayees();
            RaiseDatabaseChanged();
        }

        private void MergePayees()
        {
            foreach (var group in Payees.GroupBy(c => c.Name))
            {
                var first = group.First();
                foreach (var item in group.Skip(1))
                {
                    ReplacePayee(oldPayee: item, newPayee: first);
                    RemovePayee(item);
                }
            }
        }

        private void ReplacePayee(Payee? oldPayee, Payee? newPayee)
        {
            ReplacePayee(newPayee, p => p == oldPayee);
        }

        private void ReplacePayee(Payee? newPayee, Func<Payee?, bool> predicate)
        {
            // TODO Transaction, ScheduledTransaction, Preset, etc.
        }
    }
}
