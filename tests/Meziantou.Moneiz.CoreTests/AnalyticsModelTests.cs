using System.Globalization;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Core.Analytics;

namespace Meziantou.Moneiz.CoreTests;
public sealed class AnalyticsModelTests
{
    [Fact]
    public void ComputeModel()
    {
        var database = new Database();
        var account = new Account();
        database.Accounts.Add(account);

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 6, 1),
            Amount = 1,
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 6, 3),
            Amount = 3,
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 6, 4),
            Amount = 4,
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 6, 5),
            Amount = 5,
        });

        var result = AnalyticsModel.Build(database, new AnalyticsOptions
        {
            FromDate = new DateOnly(2023, 6, 3),
            ToDate = new DateOnly(2023, 6, 4),
            SelectedAccounts = { account },
        });

        Assert.Equal(1, result.BalanceHistory!.BalancesByAccount[0].StartBalance);
        Assert.Equal(8, result.BalanceHistory.BalancesByAccount[0].EndBalance);

        Assert.Equal(result.BigTable!.Totals[^1].Total, result.BalanceHistory.BalancesByAccount[0].Difference);
    }

    [Theory]
    [InlineData(BigTableDateGrouping.Month, "2023-01-01,2023-02-01,2023-03-01,2023-04-01,2023-05-01,2023-06-01,2023-07-01,2023-08-01,2023-09-01,2023-10-01,2023-11-01,2023-12-01", "10,0,-2,0,0,0,0,0,0,0,5,0")]
    [InlineData(BigTableDateGrouping.Quarter, "2023-01-01,2023-04-01,2023-07-01,2023-10-01", "8,0,0,5")]
    [InlineData(BigTableDateGrouping.Year, "2023-01-01", "13")]
    public void ComputeModel_BigTableGrouping(BigTableDateGrouping bigTableDateGrouping, string expectedDates, string expectedTotals)
    {
        var database = new Database();
        var account = new Account();
        database.Accounts.Add(account);

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 1, 20),
            Amount = 10m,
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 3, 10),
            Amount = -2m,
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 11, 5),
            Amount = 5m,
        });

        var result = AnalyticsModel.Build(database, new AnalyticsOptions
        {
            FromDate = new DateOnly(2023, 1, 15),
            ToDate = new DateOnly(2023, 12, 20),
            BigTableDateGrouping = bigTableDateGrouping,
            SelectedAccounts = { account },
        });

        Assert.Equal(expectedDates, string.Join(",", result.BigTable!.Dates.Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))));
        Assert.Equal(expectedTotals, string.Join(",", result.BigTable.Totals.Select(total => total.Total)));
    }

    [Fact]
    public void ComputeModel_CustomFilter_IsCombinedWithLabelFilter()
    {
        var database = new Database();
        var account = new Account();
        database.Accounts.Add(account);

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 1, 1),
            Amount = 10m,
            Labels = ["keep"],
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 1, 2),
            Amount = 6m,
            Labels = ["drop-by-label"],
        });

        database.Transactions.Add(new Transaction
        {
            Account = account,
            ValueDate = new DateOnly(2023, 1, 3),
            Amount = -4m,
            Labels = ["keep"],
        });

        var options = new AnalyticsOptions
        {
            FromDate = new DateOnly(2023, 1, 1),
            ToDate = new DateOnly(2023, 1, 3),
            TransactionFilter = static t => t.Amount > 0,
            SelectedAccounts = { account },
            SelectedLabels = { "keep" },
        };

        var result = AnalyticsModel.Build(database, options);

        Assert.Equal(10m, result.BigTable!.Totals[^1].Total);
        Assert.Equal(10m, result.BalanceHistory!.BalancesByAccount[0].Difference);
    }
}
