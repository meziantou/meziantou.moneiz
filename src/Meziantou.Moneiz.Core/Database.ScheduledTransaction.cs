using Meziantou.Framework.Scheduling;
namespace Meziantou.Moneiz.Core;

public partial class Database
{
    public ScheduledTransaction? GetScheduledTransactionById(int? id)
    {
        if (id is null)
            return null;

        return ScheduledTransactions.FirstOrDefault(item => item.Id == id);
    }

    public void SaveScheduledTransaction(ScheduledTransaction scheduledTransaction)
    {
        using (DeferEvents())
        {
            var existingTransaction = ScheduledTransactions.FirstOrDefault(item => item.Id == scheduledTransaction.Id);
            if (existingTransaction is null)
            {
                scheduledTransaction.Id = GenerateId(ScheduledTransactions, item => item.Id);
            }

            AddOrReplace(ScheduledTransactions, existingTransaction, scheduledTransaction);

            if (scheduledTransaction.NextOccurenceDate == null)
            {
                ProcessScheduledTransactions();
            }

            RaiseDatabaseChanged();
        }
    }

    public void RemoveScheduledTransaction(ScheduledTransaction scheduledTransaction)
    {
        if (ScheduledTransactions.Remove(scheduledTransaction))
        {
            RaiseDatabaseChanged();
        }
    }

    public void ProcessScheduledTransactions()
    {
        ProcessScheduledTransactions(5);
    }

    public void ProcessNextScheduledTransactionOccurrence(ScheduledTransaction scheduledTransaction)
    {
        if (scheduledTransaction.NextOccurenceDate is null)
            return;

        using (DeferEvents())
        {
            ProcessScheduledTransaction(scheduledTransaction, scheduledTransaction.NextOccurenceDate.Value.AddDays(1), forceSingleOccurrence: true);
        }
    }

    /// <summary>
    /// Computes the total amount of the scheduled transaction occurrences that impact the account
    /// up to <paramref name="date"/> and that are not materialized yet.
    /// </summary>
    private decimal GetPendingScheduledTransactionsAmount(Account account, DateOnly date)
    {
        var total = 0m;
        foreach (var scheduledTransaction in ScheduledTransactions)
        {
            var amount = GetOccurrenceAmount(scheduledTransaction, account);
            if (amount == 0)
                continue;

            total += amount * GetPendingOccurrences(scheduledTransaction, date).Count();
        }

        return total;
    }

    /// <summary>
    /// Gets the amount a single occurrence of the scheduled transaction adds to the account balance.
    /// </summary>
    private static decimal GetOccurrenceAmount(ScheduledTransaction scheduledTransaction, Account account)
    {
        var interAccount = scheduledTransaction.CreditedAccount is not null;

        var amount = 0m;
        if (scheduledTransaction.Account == account)
        {
            amount += interAccount ? -Math.Abs(scheduledTransaction.Amount) : scheduledTransaction.Amount;
        }

        if (interAccount && scheduledTransaction.CreditedAccount == account)
        {
            amount += Math.Abs(scheduledTransaction.Amount);
        }

        return amount;
    }

    private static IEnumerable<DateOnly> GetPendingOccurrences(ScheduledTransaction scheduledTransaction, DateOnly maxDate)
    {
        if (scheduledTransaction.NextOccurenceDate is null)
            return [];

        return scheduledTransaction.GetOccurrences(scheduledTransaction.NextOccurenceDate.Value).TakeWhile(date => date <= maxDate);
    }

    public void ProcessScheduledTransactions(int daysAhead)
    {
        using (DeferEvents())
        {
            var utcNow = GetToday();
            var date = utcNow.AddDays(daysAhead);

            foreach (var scheduledTransaction in ScheduledTransactions.ToList())
            {
                ProcessScheduledTransaction(scheduledTransaction, date);
            }
        }
    }

    private void ProcessScheduledTransaction(ScheduledTransaction scheduledTransaction, DateOnly createUntil, bool forceSingleOccurrence = false)
    {
        using (DeferEvents())
        {
            if (scheduledTransaction.RecurrenceRule is null)
            {
                // Invalid recurrence rule => remove the scheduled transaction
                RemoveScheduledTransaction(scheduledTransaction);
                return;
            }

            if (scheduledTransaction.NextOccurenceDate == null)
            {
                scheduledTransaction.NextOccurenceDate = scheduledTransaction.GetOccurrences(scheduledTransaction.StartDate).Cast<DateOnly?>().FirstOrDefault();
                if (scheduledTransaction.NextOccurenceDate == null)
                {
                    // recurrence ended => remove the scheduled transaction
                    RemoveScheduledTransaction(scheduledTransaction);
                    return;
                }
            }

            // Occurrences must be enumerated from the start date. Computing the next occurrence from the last occurrence
            // re-anchors the recurrence rule, e.g. "FREQ=MONTHLY" would create one transaction per day.
            using var nextOccurrences = scheduledTransaction.GetOccurrences(scheduledTransaction.NextOccurenceDate.Value.AddDays(1)).GetEnumerator();
            while (scheduledTransaction.NextOccurenceDate < createUntil || (forceSingleOccurrence && scheduledTransaction.NextOccurenceDate == createUntil))
            {
                var transactionDate = scheduledTransaction.NextOccurenceDate.Value;
                var interAccount = scheduledTransaction.CreditedAccount is not null;
                var transaction = new Transaction
                {
                    Account = scheduledTransaction.Account,
                    Category = scheduledTransaction.Category,
                    Comment = scheduledTransaction.Comment,
                    Amount = interAccount ? -Math.Abs(scheduledTransaction.Amount) : scheduledTransaction.Amount,
                    Labels = scheduledTransaction.Labels,
                    Payee = scheduledTransaction.Payee,
                    ValueDate = transactionDate,
                };

                SaveTransaction(transaction);

                if (scheduledTransaction.CreditedAccount is not null)
                {
                    var creditedTransaction = new Transaction
                    {
                        Account = scheduledTransaction.CreditedAccount,
                        Category = scheduledTransaction.Category,
                        Comment = scheduledTransaction.Comment,
                        Amount = Math.Abs(scheduledTransaction.Amount),
                        Labels = scheduledTransaction.Labels,
                        Payee = scheduledTransaction.Payee,
                        ValueDate = transactionDate,
                        LinkedTransaction = transaction,
                    };

                    transaction.LinkedTransaction = creditedTransaction;
                    SaveTransaction(transaction);
                    SaveTransaction(creditedTransaction);
                }

                if (!nextOccurrences.MoveNext())
                {
                    // Recurrence ended, remove the transaction
                    RemoveScheduledTransaction(scheduledTransaction);
                    return;
                }

                scheduledTransaction.NextOccurenceDate = nextOccurrences.Current;
                RaiseDatabaseChanged();

                if (forceSingleOccurrence)
                    return;
            }
        }
    }
}
