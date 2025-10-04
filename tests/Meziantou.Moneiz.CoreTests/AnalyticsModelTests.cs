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
}
