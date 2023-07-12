using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Meziantou.Moneiz.Core;
using Xunit;

namespace Meziantou.Moneiz.CoreTests
{
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

            Assert.Single(imported.Accounts);
            Assert.Single(imported.Categories);
            Assert.Single(imported.Payees);
            Assert.Equal(3, imported.Transactions.Count);
            Assert.NotEmpty(imported.Currencies);

            // Check references
            Assert.Same(imported.Payees.Single().DefaultCategory, imported.Categories.Single());

            Assert.Same(imported.GetCategoryById(1), imported.GetTransactionById(1).Category);
            Assert.Same(imported.GetAccountById(1), imported.GetTransactionById(1).Account);
            Assert.Same(imported.GetPayeeById(1), imported.GetTransactionById(1).Payee);

            Assert.Same(imported.GetTransactionById(2), imported.GetTransactionById(3).LinkedTransaction);
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
        public void DateOnlyJsonConverter()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new DateOnlyJsonConverter() }
            };
            var date = JsonSerializer.Deserialize<DateOnly>("\"2022-01-02T04:11:43.888Z\"", options);
            Assert.Equal(new DateOnly(2022, 01, 02), date);
        }

        [Fact]
        public void NullableDateOnlyJsonConverter()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new NullableDateOnlyJsonConverter() }
            };
            var date = JsonSerializer.Deserialize<DateOnly?>("\"2022-01-02T04:11:43.888Z\"", options);
            Assert.Equal(new DateOnly(2022, 01, 02), date);
        }
    }
}
