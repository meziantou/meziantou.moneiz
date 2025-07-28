using System;
using System.Linq;
using System.Text.Json.Serialization;
using Meziantou.Moneiz.Core.V1;

namespace Meziantou.Moneiz.Core;

public partial class Database
{
    [JsonIgnore]
    public Account? DefaultAccount => VisibleAccounts.FirstOrDefault();

    public Account? GetAccountById(int? id)
    {
        if (id is null)
            return null;

        return Accounts.FirstOrDefault(item => item.Id == id);
    }

    public void SaveAccount(Account account)
    {
        var existingAccount = Accounts.FirstOrDefault(a => a.Id == account.Id);
        if (existingAccount is null)
        {
            account.Id = GenerateId(Accounts, a => a.Id);
            if (Accounts.Count > 0)
            {
                account.SortOrder = Accounts.Where(account => !account.Closed).Max(account => account.SortOrder) + 1;
            }
        }

        AddOrReplace(Accounts, existingAccount, account);
        RaiseDatabaseChanged();
    }

    public void MoveUpAccount(Account account) => MoveAccount(account, -1);

    public void MoveDownAccount(Account account) => MoveAccount(account, 1);

    private void MoveAccount(Account account, int direction)
    {
        var accounts = Accounts.Sort().ToList();
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
        using (DeferEvents())
        {
            if (Accounts.Remove(account))
            {
                foreach (var transaction in Transactions.Where(t => t.Account == account).ToList())
                {
                    RemoveTransaction(transaction);
                }

                RaiseDatabaseChanged();
            }
        }
    }

    public decimal GetReconciledBalance(Account account)
    {
        return GetBalance(account, DateOnly.MaxValue, TransactionState.Reconciliated);
    }

    public decimal GetCheckedBalance(Account account)
    {
        return GetBalance(account, DateOnly.MaxValue, TransactionState.Checked);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0030:Do not use banned APIs", Justification = "Get user time")]
    public static DateOnly GetToday()
    {
        return DateOnly.FromDateTime(DateTime.Now);
    }

    public decimal GetTodayBalance(Account account)
    {
        return GetBalance(account, GetToday(), TransactionState.NotChecked);
    }

    public decimal GetBalance(Account account, DateOnly date)
    {
        return GetBalance(account, date, TransactionState.NotChecked);
    }

    public decimal GetBalance(Account account)
    {
        return GetBalance(account, DateOnly.MaxValue, TransactionState.NotChecked);
    }

    private decimal GetBalance(Account account, DateOnly date, TransactionState transactionState)
    {
        return account.InitialBalance + Transactions.Where(IncludeTransaction).Sum(t => t.Amount);

        bool IncludeTransaction(Transaction transaction)
        {
            if (transaction.Account != account)
                return false;

            if (transaction.ValueDate > date)
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

        RaiseDatabaseChanged();
    }
}
