using System;
using System.Linq;
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
            var category1 = new Category { Id = 1, Name = "c1", GroupName = "cg1" };
            var payee1 = new Payee { Id = 1, Name = "p1", DefaultCategory = category1 };
            var account1 = new Account { Name = "a1", CurrencyIsoCode = "USD" };
            var transaction1 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = DateTime.UtcNow, Comment = "", Id = 1, Payee = payee1, ValueDate = DateTime.UtcNow, };
            var transaction2 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = DateTime.UtcNow, Comment = "", Id = 2, Payee = payee1, ValueDate = DateTime.UtcNow, };
            var transaction3 = new Transaction { Account = account1, Amount = 1, Category = category1, CheckedDate = DateTime.UtcNow, Comment = "", Id = 3, Payee = payee1, ValueDate = DateTime.UtcNow, LinkedTransaction = transaction2 };
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
                StartDate = DateTime.UtcNow,
            };

            // Act
            db.SaveScheduledTransaction(scheduledTransaction);

            // Assert
            Assert.Equal(5, db.Transactions.Count);
        }
    }
}
