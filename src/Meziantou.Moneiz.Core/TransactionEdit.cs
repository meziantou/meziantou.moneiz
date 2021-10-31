using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
namespace Meziantou.Moneiz.Core
{
    public sealed class TransactionEdit
    {
        public int? Id { get; set; }
        public bool InterAccount { get; set; }
        [Required]
        public Account? DebitedAccount { get; set; }
        public Account? CreditedAccount { get; set; }
        public string? Payee { get; set; }
        public Category? Category { get; set; }
        public DateTime ValueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Comment { get; set; }

        public static TransactionEdit FromTransaction(Transaction transaction, bool createNewTransaction = false)
        {
            return new TransactionEdit
            {
                Id = createNewTransaction ? null : transaction.Id,
                DebitedAccount = transaction.Account,
                CreditedAccount = transaction.LinkedTransaction?.Account,
                InterAccount = transaction.LinkedTransaction != null,
                Amount = transaction.LinkedTransaction != null ? Math.Abs(transaction.Amount) : transaction.Amount,
                Category = transaction.Category,
                Comment = transaction.Comment,
                Payee = transaction.Payee?.Name,
                ValueDate = transaction.ValueDate,
            };
        }

        public static TransactionEdit FromAccount(Account? account)
        {
            return new TransactionEdit
            {
                DebitedAccount = account,
                CreditedAccount = account,
                ValueDate = DateTime.Now, // We want the local user date
                Amount = account?.DefaultCashFlow == CashFlow.Expense ? -1m : 1m,
            };
        }

        public void Save(Database database)
        {
            using (database.DeferEvents())
            {
                Debug.Assert(DebitedAccount != null);

                var transaction = database.GetDebitedTransactionById(Id);
                if (transaction == null)
                {
                    transaction = new Transaction();
                    if (DebitedAccount.DefaultTransactionState == TransactionState.Checked)
                    {
                        transaction.CheckedDate = DateTime.UtcNow;
                    }
                }

                if (!InterAccount && transaction.LinkedTransaction != null)
                {
                    database.RemoveTransaction(transaction.LinkedTransaction);
                    transaction.LinkedTransaction = null;
                }

                transaction.Account = DebitedAccount;
                transaction.Payee = database.GetOrCreatePayeeByName(Payee.TrimAndNullify());
                transaction.Category = Category;
                transaction.ValueDate = ValueDate;
                transaction.Amount = InterAccount ? -Math.Abs(Amount) : Amount;
                transaction.Comment = Comment;

                if (InterAccount)
                {
                    Debug.Assert(CreditedAccount != null);

                    var creditedTransaction = transaction.LinkedTransaction;
                    if (creditedTransaction == null)
                    {
                        creditedTransaction = new Transaction();
                        if (CreditedAccount.DefaultTransactionState == TransactionState.Checked)
                        {
                            creditedTransaction.CheckedDate = DateTime.UtcNow;
                        }
                    }

                    creditedTransaction.Account = CreditedAccount;
                    creditedTransaction.Payee = transaction.Payee;
                    creditedTransaction.Category = transaction.Category;
                    creditedTransaction.ValueDate = transaction.ValueDate;
                    creditedTransaction.Amount = Math.Abs(transaction.Amount);
                    creditedTransaction.Comment = Comment;

                    creditedTransaction.LinkedTransaction = transaction;
                    transaction.LinkedTransaction = creditedTransaction;

                    database.SaveTransaction(creditedTransaction);
                }

                database.SaveTransaction(transaction);
            }
        }
    }
}
