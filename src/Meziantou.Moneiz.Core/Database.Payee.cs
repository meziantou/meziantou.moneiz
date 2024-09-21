using System;
using System.Linq;

namespace Meziantou.Moneiz.Core;

public partial class Database
{
    public Payee? GetPayeeById(int? id)
    {
        if (id is null)
            return null;

        return Payees.FirstOrDefault(item => item.Id == id);
    }

    public Payee? GetPayeeByName(string? name)
    {
        if (name is null)
            return null;

        return Payees.FirstOrDefault(item => item.Name == name);
    }

    public Payee? GetOrCreatePayeeByName(string? name)
    {
        if (name is null)
            return null;

        var payee = Payees.FirstOrDefault(item => item.Name == name);
        if (payee is null)
        {
            payee = new Payee { Name = name };
            SavePayee(payee);
        }

        return payee;
    }

    public void RemovePayee(Payee payee)
    {
        using (DeferEvents())
        {
            ReplacePayee(oldPayee: payee, newPayee: null);
            _ = Payees.Remove(payee);
            RaiseDatabaseChanged();
        }
    }

    public void SavePayee(Payee payee)
    {
        using (DeferEvents())
        {
            var existingPayee = Payees.FirstOrDefault(item => item.Id == payee.Id);
            if (existingPayee is null)
            {
                payee.Id = GenerateId(Payees, item => item.Id);
            }

            AddOrReplace(Payees, existingPayee, payee);
            MergePayees();
            RaiseDatabaseChanged();
        }
    }

    private void MergePayees()
    {
        foreach (var group in Payees.GroupBy(c => c.Name, StringComparer.Ordinal))
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
        foreach (var scheduledTransactions in ScheduledTransactions)
        {
            if (predicate(scheduledTransactions.Payee))
            {
                scheduledTransactions.Payee = newPayee;
            }
        }

        foreach (var transactions in Transactions)
        {
            if (predicate(transactions.Payee))
            {
                transactions.Payee = newPayee;
            }
        }
    }
}
