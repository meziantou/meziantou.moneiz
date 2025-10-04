using System.Text.Json.Serialization;
using Meziantou.Framework;

namespace Meziantou.Moneiz.Core.V1;

internal sealed partial class Database
{
    [JsonPropertyName("a")]
    public IList<Account> Accounts { get; set; } = [];

    [JsonPropertyName("c")]
    public IList<Category> Categories { get; set; } = [];

    [JsonPropertyName("d")]
    public IList<Payee> Payees { get; set; } = [];

    [JsonPropertyName("e")]
    public IList<Transaction> Transactions { get; set; } = [];

    [JsonPropertyName("f")]
    public IList<ScheduledTransaction> ScheduledTransactions { get; set; } = [];

    [JsonPropertyName("g")]
    public DateTime LastModifiedDate { get; set; }

    public Core.Database ToDatabase2()
    {
        var db = new Core.Database
        {
            Accounts = Accounts,
            Categories = Categories,
        };

        db.Payees.AddRange(Payees.Select(p => new Core.Payee
        {
            Id = p.Id,
            Name = p.Name,
            DefaultCategoryId = p.DefaultCategory?.Id,
        }));

        db.Transactions.AddRange(Transactions.Select(t => new Core.Transaction
        {
            AccountId = t.Account?.Id,
            Amount = t.Amount,
            CategoryId = t.Category?.Id,
            CheckedDate = t.CheckedDate,
            Comment = t.Comment,
            Id = t.Id,
            LinkedTransactionId = t.LinkedTransaction?.Id,
            PayeeId = t.Payee?.Id,
            ReconciliationDate = t.ReconciliationDate,
            ValueDate = t.ValueDate,
        }));

        db.ScheduledTransactions.AddRange(ScheduledTransactions.Select(t => new Core.ScheduledTransaction
        {
            AccountId = t.Account?.Id,
            Amount = t.Amount,
            CategoryId = t.Category?.Id,
            CreditedAccountId = t.CreditedAccount?.Id,
            Id = t.Id,
            Name = t.Name,
            NextOccurenceDate = t.NextOccurenceDate,
            PayeeId = t.Payee?.Id,
            RecurrenceRuleText = t.RecurrenceRuleText,
            StartDate = t.StartDate,
            Comment = t.Comment,
        }));

        return db;
    }
}
