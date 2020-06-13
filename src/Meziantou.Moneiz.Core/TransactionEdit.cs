using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Meziantou.Moneiz.Core
{
    public sealed class TransactionEdit
    {
        public bool InterAccount { get; set; }
        [Required]
        public Account? DebitedAccount { get; set; }
        public Account? CreditedAccount { get; set; }
        public string? Payee { get; set; }
        public Category? Category { get; set; }
        public DateTime ValueDate { get; set; }
        public decimal Amount { get; set; }
        public string? Comment { get; set; }

        public void Save(Database database, int? id)
        {
            using (database.DeferEvents())
            {
                Debug.Assert(DebitedAccount != null);

                var transaction = database.GetDebitedTransactionById(id);
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
