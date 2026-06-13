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
        var transaction1 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = today, Comment = "", Id = 1, Payee = payee1, ValueDate = today, };
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
        Assert.Equal(3, imported.Transactions.Count);
        Assert.NotEmpty(imported.Currencies);

        // Check references
        Assert.Same(imported.Payees.Single().DefaultCategory, imported.Categories.Single());

        Assert.Same(imported.GetCategoryById(1), imported.GetTransactionById(1)!.Category);
        Assert.Same(imported.GetAccountById(1), imported.GetTransactionById(1)!.Account);
        Assert.Same(imported.GetPayeeById(1), imported.GetTransactionById(1)!.Payee);

        Assert.Same(imported.GetTransactionById(2), imported.GetTransactionById(3)!.LinkedTransaction);
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
        Assert.Equal(5, db.Transactions.Count);
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
}
