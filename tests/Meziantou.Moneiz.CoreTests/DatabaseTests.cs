using System.Text.Json;
using Meziantou.Moneiz.Core;

namespace Meziantou.Moneiz.CoreTests;

public class DatabaseTests
{
    [Fact]
    public async Task ImportExport()
    {
        // Arrange
        var today = Database.GetToday();
        var category1 = new Category { Id = 1, Name = "c1", GroupName = "cg1" };
        var payee1 = new Payee { Id = 1, Name = "p1", DefaultCategory = category1 };
        var account1 = new Account { Id = 1, Name = "a1", CurrencyIsoCode = "USD" };
        var transaction1 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = today, Comment = "", Id = 1, Payee = payee1, ValueDate = today, Labels = ["tag1", "tag2"] };
        var transaction2 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = today, Comment = "", Id = 2, Payee = payee1, ValueDate = today, };
        var transaction3 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = today, Comment = "", Id = 3, Payee = payee1, ValueDate = today, LinkedTransaction = transaction2 };
        transaction2.LinkedTransaction = transaction3;

        var database = new Database()
        {
            Categories = { category1 },
            Payees = { payee1 },
            Accounts = { account1 },
            Transactions = { transaction1, transaction2, transaction3 },
        };

        // Act
        var str = database.Export();
        var imported = await Database.Load(str);
        database = null; // avoid using it in the asserts

        // Assert
        Assert.Null(database);

        _ = Assert.Single(imported.Accounts);
        _ = Assert.Single(imported.Categories);
        _ = Assert.Single(imported.Payees);
        Assert.HasCount(3, imported.Transactions);
        Assert.NotEmpty(imported.Currencies);

        // Check references
        Assert.Same(imported.Payees.Single().DefaultCategory, imported.Categories.Single());

        Assert.Same(imported.GetCategoryById(1), imported.GetTransactionById(1)!.Category);
        Assert.Same(imported.GetAccountById(1), imported.GetTransactionById(1)!.Account);
        Assert.Same(imported.GetPayeeById(1), imported.GetTransactionById(1)!.Payee);

        Assert.Same(imported.GetTransactionById(2), imported.GetTransactionById(3)!.LinkedTransaction);
        Assert.Equal((IEnumerable<string>)["tag1", "tag2"], imported.GetTransactionById(1)!.Labels);
        Assert.Null(imported.GetTransactionById(2)!.Labels);
    }

    [Fact]
    public async Task ImportExportResolvesEveryTransferReference()
    {
        const int TransferCount = 500;

        var today = Database.GetToday();
        var debitedAccount = new Account { Id = 1, Name = "a1", CurrencyIsoCode = "USD" };
        var creditedAccount = new Account { Id = 2, Name = "a2", CurrencyIsoCode = "USD" };
        var database = new Database()
        {
            Accounts = { debitedAccount, creditedAccount },
        };

        for (var i = 0; i < TransferCount; i++)
        {
            var debit = new Transaction { Id = (i * 2) + 1, Account = debitedAccount, Amount = -10, ValueDate = today };
            var credit = new Transaction { Id = (i * 2) + 2, Account = creditedAccount, Amount = 10, ValueDate = today };
            debit.LinkedTransaction = credit;
            credit.LinkedTransaction = debit;
            database.Transactions.Add(debit);
            database.Transactions.Add(credit);
        }

        var imported = await Database.Load(database.Export());

        Assert.HasCount(TransferCount * 2, imported.Transactions);
        foreach (var transaction in imported.Transactions)
        {
            var linkedTransaction = transaction.LinkedTransaction;
            Assert.NotNull(linkedTransaction);
            Assert.Equal(transaction.Amount < 0 ? transaction.Id + 1 : transaction.Id - 1, linkedTransaction.Id);
            Assert.Same(transaction, linkedTransaction.LinkedTransaction);
            Assert.Contains(linkedTransaction, imported.Transactions);
            Assert.Same(transaction.Amount < 0 ? imported.GetAccountById(1) : imported.GetAccountById(2), transaction.Account);
        }
    }

    [Fact]
    public void AddScheduledTransaction()
    {
        var db = new Database();
        var account = new Account();
        db.SaveAccount(account);

        var scheduledTransaction = new ScheduledTransaction
        {
            Account = account,
            Amount = 1,
            RecurrenceRuleText = "FREQ=daily",
            Name = "test",
            StartDate = Database.GetToday(),
        };

        // Act
        db.SaveScheduledTransaction(scheduledTransaction);

        // Assert
        Assert.HasCount(5, db.Transactions);
    }

    [Fact]
    public void ScheduledTransaction_LabelsAreCopiedToGeneratedTransactions()
    {
        var db = new Database();
        var account = new Account();
        db.SaveAccount(account);

        var scheduledTransaction = new ScheduledTransaction
        {
            Account = account,
            Amount = 1,
            RecurrenceRuleText = "FREQ=daily",
            Name = "test",
            StartDate = Database.GetToday(),
            Labels = ["tag1", "tag2"],
        };

        db.SaveScheduledTransaction(scheduledTransaction);

        Assert.All(db.Transactions, t => Assert.Equal((IEnumerable<string>)["tag1", "tag2"], t.Labels));
    }

    [Fact]
    public void ProcessNextScheduledTransactionOccurrence_UsesTheActualNextOccurrence()
    {
        var db = new Database();
        var account = new Account();
        db.SaveAccount(account);

        var scheduledTransaction = new ScheduledTransaction
        {
            Account = account,
            Amount = 1,
            RecurrenceRuleText = "FREQ=MONTHLY;BYMONTHDAY=10",
            Name = "test",
            StartDate = new DateOnly(2026, 08, 10),
            NextOccurenceDate = new DateOnly(2026, 08, 10),
        };

        db.SaveScheduledTransaction(scheduledTransaction);
        db.Transactions.Clear();

        db.ProcessNextScheduledTransactionOccurrence(scheduledTransaction);

        var transaction = Assert.Single(db.Transactions);
        Assert.Equal(new DateOnly(2026, 08, 10), transaction.ValueDate);
        Assert.Equal(new DateOnly(2026, 09, 10), scheduledTransaction.NextOccurenceDate);
    }

    [Fact]
    public void GetPayeeSuggestionsMatchesAndRanksNames()
    {
        var account = new Account { Id = 1 };
        var database = new Database
        {
            Payees =
            {
                new Payee { Id = 1, Name = "Café Central" },
                new Payee { Id = 2, Name = "Central Market" },
                new Payee { Id = 3, Name = "The Central Café" },
                new Payee { Id = 4, Name = "Coffee Shop" },
            },
        };

        Assert.Equal(["Café Central", "The Central Café"], database.GetPayeeSuggestions(account, "cafe central", 10).Select(payee => payee.Name));
        Assert.Equal(["Central Market", "Café Central", "The Central Café"], database.GetPayeeSuggestions(account, "central", 10).Select(payee => payee.Name));
        Assert.Equal(["The Central Café"], database.GetPayeeSuggestions(account, "cafe the", 10).Select(payee => payee.Name));
        Assert.Equal(["Coffee Shop"], database.GetPayeeSuggestions(account, "cofee", 10).Select(payee => payee.Name));
        Assert.Empty(database.GetPayeeSuggestions(account, "cxfx", 10));
    }

    [Fact]
    public void GetPayeeSuggestionsUsesAccountUsageAsTieBreaker()
    {
        var selectedAccount = new Account { Id = 1 };
        var otherAccount = new Account { Id = 2 };
        var popular = new Payee { Id = 1, Name = "Popular Market" };
        var recent = new Payee { Id = 2, Name = "Recent Market" };
        var older = new Payee { Id = 3, Name = "Older Market" };
        var unused = new Payee { Id = 4, Name = "Unused Market" };
        var otherAccountPayee = new Payee { Id = 5, Name = "Other Market" };
        var exact = new Payee { Id = 6, Name = "Market" };
        var database = new Database
        {
            Accounts = { selectedAccount, otherAccount },
            Payees = { popular, recent, older, unused, otherAccountPayee, exact },
            Transactions =
            {
                new Transaction { Account = selectedAccount, Payee = popular, ValueDate = new DateOnly(2025, 1, 1) },
                new Transaction { Account = selectedAccount, Payee = popular, ValueDate = new DateOnly(2025, 1, 2) },
                new Transaction { Account = selectedAccount, Payee = recent, ValueDate = new DateOnly(2025, 2, 1) },
                new Transaction { Account = selectedAccount, Payee = older, ValueDate = new DateOnly(2025, 1, 1) },
                new Transaction { Account = otherAccount, Payee = otherAccountPayee, ValueDate = new DateOnly(2025, 3, 1) },
            },
        };

        Assert.Equal(
            ["Popular Market", "Recent Market", "Older Market", "Market"],
            database.GetPayeeSuggestions(selectedAccount, null, 4).Select(payee => payee.Name));
        Assert.Equal(
            ["Market", "Popular Market", "Recent Market", "Older Market"],
            database.GetPayeeSuggestions(selectedAccount, "market", 4).Select(payee => payee.Name));
        Assert.Empty(database.GetPayeeSuggestions(selectedAccount, null, 0));
    }

    [Fact]
    public void SaveAccountWhenAllExistingAccountsAreClosed()
    {
        var database = new Database();
        var closedAccount = new Account { Name = "Closed account", Closed = true };
        database.SaveAccount(closedAccount);

        var newAccount = new Account { Name = "New account" };
        database.SaveAccount(newAccount);

        Assert.HasCount(2, database.Accounts);
        Assert.Equal([newAccount], database.VisibleAccounts);
        Assert.Equal(0, newAccount.SortOrder);
    }

    [Fact]
    public void MoveAccountBeforeReordersAccounts()
    {
        var database = new Database();
        var account1 = new Account { Name = "Account 1" };
        var account2 = new Account { Name = "Account 2" };
        var account3 = new Account { Name = "Account 3" };
        database.SaveAccount(account1);
        database.SaveAccount(account2);
        database.SaveAccount(account3);

        var updated = database.MoveAccountBefore(account3, account1);

        Assert.True(updated);
        Assert.Equal([account3, account1, account2], database.VisibleAccounts);
    }

    [Fact]
    public void MoveAccountAfterReordersAccounts()
    {
        var database = new Database();
        var account1 = new Account { Name = "Account 1" };
        var account2 = new Account { Name = "Account 2" };
        var account3 = new Account { Name = "Account 3" };
        database.SaveAccount(account1);
        database.SaveAccount(account2);
        database.SaveAccount(account3);

        var updated = database.MoveAccountAfter(account1, account3);

        Assert.True(updated);
        Assert.Equal([account2, account3, account1], database.VisibleAccounts);
    }

    [Fact]
    public void MoveAccountAfterDoesNotMoveAcrossOpenAndClosedAccounts()
    {
        var database = new Database();
        var openedAccount = new Account { Name = "Opened account" };
        var closedAccount = new Account { Name = "Closed account", Closed = true };
        database.SaveAccount(openedAccount);
        database.SaveAccount(closedAccount);

        var updated = database.MoveAccountAfter(openedAccount, closedAccount);

        Assert.False(updated);
        Assert.Equal([openedAccount, closedAccount], database.Accounts.Sort());
    }

    [Fact]
    public void MoveAccountBeforeDoesNotMoveAcrossOpenAndClosedAccounts()
    {
        var database = new Database();
        var openedAccount = new Account { Name = "Opened account" };
        var closedAccount = new Account { Name = "Closed account", Closed = true };
        database.SaveAccount(openedAccount);
        database.SaveAccount(closedAccount);

        var updated = database.MoveAccountBefore(closedAccount, openedAccount);

        Assert.False(updated);
        Assert.Equal([openedAccount, closedAccount], database.Accounts.Sort());
    }

    [Fact]
    [SuppressMessage("Performance", "CA1869:Cache and reuse 'JsonSerializerOptions' instances")]
    public void DateOnlyJsonConverter()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new DateOnlyJsonConverter() },
        };
        var date = JsonSerializer.Deserialize<DateOnly>("\"2022-01-02T04:11:43.888Z\"", options);
        Assert.Equal(new DateOnly(2022, 01, 02), date);
    }

    [Fact]
    [SuppressMessage("Performance", "CA1869:Cache and reuse 'JsonSerializerOptions' instances")]
    public void NullableDateOnlyJsonConverter()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new NullableDateOnlyJsonConverter() },
        };
        var date = JsonSerializer.Deserialize<DateOnly?>("\"2022-01-02T04:11:43.888Z\"", options);
        Assert.Equal(new DateOnly(2022, 01, 02), date);
    }

    [Fact]
    public void RenameLabel_UpdatesAllMatchingTransactions()
    {
        var account = new Account { Id = 1 };
        var t1 = new Transaction { Id = 1, Account = account, Amount = 1, ValueDate = Database.GetToday(), Labels = ["old", "other"] };
        var t2 = new Transaction { Id = 2, Account = account, Amount = 2, ValueDate = Database.GetToday(), Labels = ["old"] };
        var t3 = new Transaction { Id = 3, Account = account, Amount = 3, ValueDate = Database.GetToday(), Labels = ["unrelated"] };

        var db = new Database
        {
            Accounts = { account },
            Transactions = { t1, t2, t3 },
        };

        db.RenameLabel("old", "new");

        Assert.Equal((IEnumerable<string>)["new", "other"], db.GetTransactionById(1)!.Labels);
        Assert.Equal((IEnumerable<string>)["new"], db.GetTransactionById(2)!.Labels);
        Assert.Equal((IEnumerable<string>)["unrelated"], db.GetTransactionById(3)!.Labels);
    }

    [Fact]
    public void RenameLabel_DeduplicatesWhenNewNameAlreadyPresent()
    {
        var account = new Account { Id = 1 };
        var t1 = new Transaction { Id = 1, Account = account, Amount = 1, ValueDate = Database.GetToday(), Labels = ["old", "new"] };

        var db = new Database
        {
            Accounts = { account },
            Transactions = { t1 },
        };

        db.RenameLabel("old", "new");

        Assert.Equal((IEnumerable<string>)["new"], db.GetTransactionById(1)!.Labels);
    }

    [Fact]
    public void DeleteLabel_RemovesLabelFromAllTransactions()
    {
        var account = new Account { Id = 1 };
        var t1 = new Transaction { Id = 1, Account = account, Amount = 1, ValueDate = Database.GetToday(), Labels = ["remove", "keep"] };
        var t2 = new Transaction { Id = 2, Account = account, Amount = 2, ValueDate = Database.GetToday(), Labels = ["remove"] };
        var t3 = new Transaction { Id = 3, Account = account, Amount = 3, ValueDate = Database.GetToday(), Labels = ["keep"] };

        var db = new Database
        {
            Accounts = { account },
            Transactions = { t1, t2, t3 },
        };

        db.DeleteLabel("remove");

        Assert.Equal((IEnumerable<string>)["keep"], db.GetTransactionById(1)!.Labels);
        Assert.Null(db.GetTransactionById(2)!.Labels);
        Assert.Equal((IEnumerable<string>)["keep"], db.GetTransactionById(3)!.Labels);
    }

    [Fact]
    public void DuplicateTransfer_FromCreditedTransaction_KeepsTheDirection()
    {
        var source = new Account { Id = 1, Name = "Source" };
        var destination = new Account { Id = 2, Name = "Destination" };
        var debitedTransaction = new Transaction { Id = 1, Account = source, Amount = -100, ValueDate = new DateOnly(2026, 01, 01) };
        var creditedTransaction = new Transaction { Id = 2, Account = destination, Amount = 100, ValueDate = new DateOnly(2026, 01, 01), LinkedTransaction = debitedTransaction };
        debitedTransaction.LinkedTransaction = creditedTransaction;

        var db = new Database
        {
            Accounts = { source, destination },
            Transactions = { debitedTransaction, creditedTransaction },
        };

        var duplicate = TransactionEdit.FromTransaction(creditedTransaction, createNewTransaction: true);
        duplicate.Save(db);

        Assert.HasCount(4, db.Transactions);
        Assert.Equal(-200, db.GetBalance(source));
        Assert.Equal(200, db.GetBalance(destination));
    }

    [Fact]
    public void DuplicateTransfer_FromDebitedTransaction_KeepsTheDirection()
    {
        var source = new Account { Id = 1, Name = "Source" };
        var destination = new Account { Id = 2, Name = "Destination" };
        var debitedTransaction = new Transaction { Id = 1, Account = source, Amount = -100, ValueDate = new DateOnly(2026, 01, 01) };
        var creditedTransaction = new Transaction { Id = 2, Account = destination, Amount = 100, ValueDate = new DateOnly(2026, 01, 01), LinkedTransaction = debitedTransaction };
        debitedTransaction.LinkedTransaction = creditedTransaction;

        var db = new Database
        {
            Accounts = { source, destination },
            Transactions = { debitedTransaction, creditedTransaction },
        };

        var duplicate = TransactionEdit.FromTransaction(debitedTransaction, createNewTransaction: true);
        duplicate.Save(db);

        Assert.HasCount(4, db.Transactions);
        Assert.Equal(-200, db.GetBalance(source));
        Assert.Equal(200, db.GetBalance(destination));
    }

    [Fact]
    public void GetAllLabels_ReturnsDistinctSortedLabels()
    {
        var account = new Account { Id = 1 };
        var t1 = new Transaction { Id = 1, Account = account, Amount = 1, ValueDate = Database.GetToday(), Labels = ["beta", "alpha"] };
        var t2 = new Transaction { Id = 2, Account = account, Amount = 2, ValueDate = Database.GetToday(), Labels = ["beta", "gamma"] };
        var t3 = new Transaction { Id = 3, Account = account, Amount = 3, ValueDate = Database.GetToday() };

        var db = new Database
        {
            Accounts = { account },
            Transactions = { t1, t2, t3 },
        };

        var labels = db.GetAllLabels().ToList();

        Assert.Equal((IEnumerable<string>)["alpha", "beta", "gamma"], labels);
    }

    [Fact]
    public void RemoveAccount_RemovesScheduledTransactionsOfTheAccount()
    {
        var db = new Database();
        var account = new Account { Name = "a1" };
        db.SaveAccount(account);

        db.SaveScheduledTransaction(new ScheduledTransaction
        {
            Account = account,
            Amount = 1,
            RecurrenceRuleText = "FREQ=daily",
            Name = "test",
            StartDate = Database.GetToday(),
        });

        db.RemoveAccount(account);

        Assert.Empty(db.ScheduledTransactions);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public void RemoveAccount_RemovesScheduledTransactionsCreditingTheAccount()
    {
        var db = new Database();
        var debitedAccount = new Account { Name = "a1" };
        var creditedAccount = new Account { Name = "a2" };
        db.SaveAccount(debitedAccount);
        db.SaveAccount(creditedAccount);

        db.SaveScheduledTransaction(new ScheduledTransaction
        {
            Account = debitedAccount,
            CreditedAccount = creditedAccount,
            Amount = 1,
            RecurrenceRuleText = "FREQ=daily",
            Name = "test",
            StartDate = Database.GetToday(),
        });

        db.RemoveAccount(creditedAccount);

        Assert.Empty(db.ScheduledTransactions);
    }

    [Fact]
    public async Task RemoveAccount_ScheduledTransactionsAreNotReattachedToANewAccountAfterReload()
    {
        var db = new Database();
        var account = new Account { Name = "a1" };
        db.SaveAccount(account);

        db.SaveScheduledTransaction(new ScheduledTransaction
        {
            Account = account,
            Amount = 1,
            RecurrenceRuleText = "FREQ=daily",
            Name = "test",
            StartDate = Database.GetToday(),
        });

        db.RemoveAccount(account);

        var newAccount = new Account { Name = "a2" };
        db.SaveAccount(newAccount);
        Assert.Equal(account.Id, newAccount.Id);

        var imported = await Database.Load(db.Export());

        Assert.Empty(imported.ScheduledTransactions);
        Assert.Empty(imported.Transactions);
    }

    [Fact]
    public void TransactionEdit_InlineEditOfCreditedTransfer_KeepsEachSideOnItsOwnAccount()
    {
        var source = new Account { Id = 1, Name = "source" };
        var destination = new Account { Id = 2, Name = "destination" };
        var today = Database.GetToday();
        var reconciliationDate = new DateTime(2026, 01, 02, 03, 04, 05, DateTimeKind.Utc);
        var debited = new Transaction { Id = 1, Account = source, Amount = -100, ValueDate = today, CheckedDate = today, ReconciliationDate = reconciliationDate };
        var credited = new Transaction { Id = 2, Account = destination, Amount = 100, ValueDate = today, LinkedTransaction = debited };
        debited.LinkedTransaction = credited;

        var db = new Database
        {
            Accounts = { source, destination },
            Transactions = { debited, credited },
        };

        var edit = TransactionEdit.FromTransaction(credited, editCurrentTransaction: true);
        edit.Comment = "updated";
        edit.Save(db);

        Assert.Same(source, db.GetTransactionById(1)!.Account);
        Assert.Equal(-100, db.GetTransactionById(1)!.Amount);
        Assert.Equal(reconciliationDate, db.GetTransactionById(1)!.ReconciliationDate);

        Assert.Same(destination, db.GetTransactionById(2)!.Account);
        Assert.Equal(100, db.GetTransactionById(2)!.Amount);
        Assert.Null(db.GetTransactionById(2)!.ReconciliationDate);

        Assert.Equal("updated", db.GetTransactionById(1)!.Comment);
        Assert.Equal("updated", db.GetTransactionById(2)!.Comment);
    }

    [Fact]
    public void TransactionEdit_InlineEditOfDebitedTransfer_KeepsEachSideOnItsOwnAccount()
    {
        var source = new Account { Id = 1, Name = "source" };
        var destination = new Account { Id = 2, Name = "destination" };
        var today = Database.GetToday();
        var reconciliationDate = new DateTime(2026, 01, 02, 03, 04, 05, DateTimeKind.Utc);
        var debited = new Transaction { Id = 1, Account = source, Amount = -100, ValueDate = today, CheckedDate = today, ReconciliationDate = reconciliationDate };
        var credited = new Transaction { Id = 2, Account = destination, Amount = 100, ValueDate = today, LinkedTransaction = debited };
        debited.LinkedTransaction = credited;

        var db = new Database
        {
            Accounts = { source, destination },
            Transactions = { debited, credited },
        };

        var edit = TransactionEdit.FromTransaction(debited, editCurrentTransaction: true);
        edit.Comment = "updated";
        edit.Save(db);

        Assert.Same(source, db.GetTransactionById(1)!.Account);
        Assert.Equal(-100, db.GetTransactionById(1)!.Amount);
        Assert.Equal(reconciliationDate, db.GetTransactionById(1)!.ReconciliationDate);

        Assert.Same(destination, db.GetTransactionById(2)!.Account);
        Assert.Equal(100, db.GetTransactionById(2)!.Amount);

        Assert.Equal("updated", db.GetTransactionById(1)!.Comment);
        Assert.Equal("updated", db.GetTransactionById(2)!.Comment);
    }

    [Fact]
    public void TransactionEdit_InlineEditAmountOfCreditedTransfer_UpdatesBothSides()
    {
        var source = new Account { Id = 1, Name = "source" };
        var destination = new Account { Id = 2, Name = "destination" };
        var today = Database.GetToday();
        var debited = new Transaction { Id = 1, Account = source, Amount = -100, ValueDate = today };
        var credited = new Transaction { Id = 2, Account = destination, Amount = 100, ValueDate = today, LinkedTransaction = debited };
        debited.LinkedTransaction = credited;

        var db = new Database
        {
            Accounts = { source, destination },
            Transactions = { debited, credited },
        };

        var edit = TransactionEdit.FromTransaction(credited, editCurrentTransaction: true);
        edit.Amount = 150;
        edit.Save(db);

        Assert.Same(source, db.GetTransactionById(1)!.Account);
        Assert.Equal(-150, db.GetTransactionById(1)!.Amount);

        Assert.Same(destination, db.GetTransactionById(2)!.Account);
        Assert.Equal(150, db.GetTransactionById(2)!.Amount);
    }

    [Fact]
    public void GetProjectedBalance_IncludesScheduledTransactionsBeyondTheMaterializedWindow()
    {
        var db = new Database();
        var account = new Account { InitialBalance = 100 };
        db.SaveAccount(account);

        var scheduledTransaction = new ScheduledTransaction
        {
            Account = account,
            Amount = -200,
            RecurrenceRuleText = "FREQ=MONTHLY;BYMONTHDAY=10",
            Name = "test",
            StartDate = new DateOnly(2026, 08, 10),
            NextOccurenceDate = new DateOnly(2026, 08, 10),
        };

        db.SaveScheduledTransaction(scheduledTransaction);

        Assert.Empty(db.Transactions);
        Assert.Equal(100, db.GetBalance(account, new DateOnly(2026, 09, 10)));

        Assert.Equal(100, db.GetProjectedBalance(account, new DateOnly(2026, 08, 09)));
        Assert.Equal(-100, db.GetProjectedBalance(account, new DateOnly(2026, 08, 10)));
        Assert.Equal(-300, db.GetProjectedBalance(account, new DateOnly(2026, 09, 10)));

        // The projection must not materialize anything
        Assert.Empty(db.Transactions);
        Assert.Equal(new DateOnly(2026, 08, 10), scheduledTransaction.NextOccurenceDate);
    }

    [Fact]
    public void GetProjectedBalance_DoesNotCountMaterializedOccurrencesTwice()
    {
        var today = Database.GetToday();
        var db = new Database();
        var account = new Account();
        db.SaveAccount(account);

        db.SaveScheduledTransaction(new ScheduledTransaction
        {
            Account = account,
            Amount = -1,
            RecurrenceRuleText = "FREQ=DAILY",
            Name = "test",
            StartDate = today,
        });

        Assert.HasCount(5, db.Transactions);
        Assert.Equal(-5, db.GetBalance(account, today.AddDays(9)));

        Assert.Equal(-5, db.GetProjectedBalance(account, today.AddDays(4)));
        Assert.Equal(-10, db.GetProjectedBalance(account, today.AddDays(9)));
    }

    [Fact]
    public void GetProjectedBalance_HandlesInterAccountScheduledTransactions()
    {
        var db = new Database();
        var debitedAccount = new Account { Name = "debited", InitialBalance = 100 };
        var creditedAccount = new Account { Name = "credited" };
        var otherAccount = new Account { Name = "other" };
        db.SaveAccount(debitedAccount);
        db.SaveAccount(creditedAccount);
        db.SaveAccount(otherAccount);

        db.SaveScheduledTransaction(new ScheduledTransaction
        {
            Account = debitedAccount,
            CreditedAccount = creditedAccount,
            Amount = 30,
            RecurrenceRuleText = "FREQ=MONTHLY;BYMONTHDAY=10",
            Name = "test",
            StartDate = new DateOnly(2026, 08, 10),
            NextOccurenceDate = new DateOnly(2026, 08, 10),
        });

        Assert.Equal(100, db.GetProjectedBalance(debitedAccount, new DateOnly(2026, 08, 09)));
        Assert.Equal(70, db.GetProjectedBalance(debitedAccount, new DateOnly(2026, 08, 10)));
        Assert.Equal(30, db.GetProjectedBalance(creditedAccount, new DateOnly(2026, 08, 10)));
        Assert.Equal(0, db.GetProjectedBalance(otherAccount, new DateOnly(2026, 09, 10)));
    }
}
