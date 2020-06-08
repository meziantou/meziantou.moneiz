using System;
using System.Linq;

namespace Meziantou.Moneiz.Core
{
    partial class Database
    {
        public ScheduledTransaction? GetScheduledTransactionById(int? id)
        {
            if (id == null)
                return null;

            return ScheduledTransactions.FirstOrDefault(item => item.Id == id);
        }

        public void SaveScheduledTransaction(ScheduledTransaction scheduledTransaction)
        {
            var existingTransaction = ScheduledTransactions.FirstOrDefault(item => item.Id == scheduledTransaction.Id);
            if (existingTransaction == null)
            {
                scheduledTransaction.Id = GenerateId(ScheduledTransactions, item => item.Id);
            }

            AddOrReplace(ScheduledTransactions, existingTransaction, scheduledTransaction);
            RaiseDatabaseChanged();

            if (scheduledTransaction.NextOccurenceDate == null)
            {
                ProcessScheduledTransactions();
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
            var utcNow = DateTime.UtcNow.Date;
            var date = utcNow.AddDays(5);

            foreach (var scheduledTransaction in ScheduledTransactions.ToList())
            {
                ProcessScheduledTransaction(scheduledTransaction, date);
            }
        }

        private void ProcessScheduledTransaction(ScheduledTransaction scheduledTransaction, DateTime createUntil)
        {
            var reccurenceRule = scheduledTransaction.RecurrenceRule;
            if (reccurenceRule == null)
            {
                // Invalid recurrence rule => remove the scheduled transaction
                RemoveScheduledTransaction(scheduledTransaction);
                return;
            }

            if (scheduledTransaction.NextOccurenceDate == null)
            {
                var recurrenceDate = reccurenceRule.GetNextOccurrence(scheduledTransaction.StartDate);
                scheduledTransaction.NextOccurenceDate = recurrenceDate;
                if (scheduledTransaction.NextOccurenceDate == null)
                {
                    // recurrence ended => remove the scheduled transaction
                    RemoveScheduledTransaction(scheduledTransaction);
                    return;
                }
            }

            while (scheduledTransaction.NextOccurenceDate < createUntil)
            {
                var transactionDate = scheduledTransaction.NextOccurenceDate.Value;
                var interAccount = scheduledTransaction.CreditedAccount != null;
                var transaction = new Transaction
                {
                    Account = scheduledTransaction.Account,
                    Category = scheduledTransaction.Category,
                    Comment = scheduledTransaction.Comment,
                    Amount = interAccount ? -Math.Abs(scheduledTransaction.Amount) : scheduledTransaction.Amount,
                    Payee = scheduledTransaction.Payee,
                    ValueDate = transactionDate,
                };

                SaveTransaction(transaction);

                if (scheduledTransaction.CreditedAccount != null)
                {
                    var creditedTransaction = new Transaction
                    {
                        Account = scheduledTransaction.CreditedAccount,
                        Category = scheduledTransaction.Category,
                        Comment = scheduledTransaction.Comment,
                        Amount = Math.Abs(scheduledTransaction.Amount),
                        Payee = scheduledTransaction.Payee,
                        ValueDate = transactionDate,
                        LinkedTransaction = transaction,
                    };

                    transaction.LinkedTransaction = creditedTransaction;
                    SaveTransaction(transaction);
                    SaveTransaction(creditedTransaction);
                }

                var newRecurrenceDate = reccurenceRule.GetNextOccurrence(scheduledTransaction.NextOccurenceDate.Value.AddDays(1));
                if (scheduledTransaction.NextOccurenceDate == newRecurrenceDate)
                {
                    // Infinite loop, remove the transaction
                    RemoveScheduledTransaction(scheduledTransaction);
                    return;
                }

                if (newRecurrenceDate == null)
                {
                    // Recurrence ended, remove the transaction
                    RemoveScheduledTransaction(scheduledTransaction);
                    return;
                }

                scheduledTransaction.NextOccurenceDate = newRecurrenceDate;
                RaiseDatabaseChanged();
            }
        }
    }
}
