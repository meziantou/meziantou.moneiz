using Meziantou.AspNetCore.Testing;
using static Meziantou.AspNetCore.Testing.Expectations;

namespace Meziantou.Moneiz.E2ETests;

public sealed class EndToEndTests
{
    private static readonly DateOnly Today = new(2026, 1, 10);

    [Fact]
    public async Task CreateAccount_ListsAccount()
    {
        await using var app = await MoneizApp.StartAsync(Today);

        await app.CreateAccountAsync("Checking", 100m);

        await Expect(app.GetAccountRow("Checking")).ToContainTextAsync("USD", cancellationToken: XunitCancellationToken);
        var sidebarAccount = app.Page.Locator(".widget-accounts-summary").Filter(new LocatorFilterOptions { HasText = "Checking" });
        await Expect(sidebarAccount.Locator(".amount-value")).ToHaveTextAsync("100.00", cancellationToken: XunitCancellationToken);
    }

    [Fact]
    public async Task CreateTransactions_UpdatesBalances()
    {
        await using var app = await MoneizApp.StartAsync(Today);
        await app.CreateAccountAsync("Checking", 100m);
        await app.OpenAccountAsync("Checking");
        await app.ExpectBalancesAsync(today: 100m, all: 100m, reconciled: 100m, @checked: 100m);

        await app.CreateTransactionAsync("Grocery", -30m, Today);
        await app.CreateTransactionAsync("Salary", 50m, Today.AddDays(1));
        await app.CreateTransactionAsync("Rent", -1200.5m, Today.AddDays(-3));

        await Expect(app.TransactionRows).ToHaveCountAsync(3, cancellationToken: XunitCancellationToken);
        await Expect(app.GetTransactionRow("Grocery").Locator("td[data-column=amount] .amount-value")).ToHaveTextAsync("-30.00", cancellationToken: XunitCancellationToken);
        await app.ExpectBalancesAsync(today: -1130.5m, all: -1080.5m, reconciled: 100m, @checked: 100m);
    }

    [Fact]
    public async Task InterAccountTransfer_UpdatesBothAccounts()
    {
        await using var app = await MoneizApp.StartAsync(Today);
        await app.CreateAccountAsync("Checking", 1000m);
        await app.CreateAccountAsync("Savings", 0m);

        await app.CreateTransferAsync("Checking", "Savings", 250m, Today);

        await app.ExpectBalancesAsync(today: 750m, all: 750m, reconciled: 1000m, @checked: 1000m);
        await app.OpenAccountAsync("Savings");
        await app.ExpectBalancesAsync(today: 250m, all: 250m, reconciled: 0m, @checked: 0m);
    }

    [Fact]
    public async Task ScheduledTransaction_IsInstantiatedWhenTimeAdvances()
    {
        await using var app = await MoneizApp.StartAsync(Today);
        await app.CreateAccountAsync("Checking", 100m);

        // Monthly on the 14th: the occurrences before today + 5 days are created when the schedule is saved
        await app.CreateScheduledTransactionAsync("Internet", "Checking", new DateOnly(2026, 1, 14), "FREQ=MONTHLY;BYMONTHDAY=14;INTERVAL=1", -20m);
        await Expect(app.GetScheduledTransactionRow("Internet")).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);

        await app.OpenAccountAsync("Checking");
        await Expect(app.GetTransactionRow("Internet")).ToHaveCountAsync(1, cancellationToken: XunitCancellationToken);
        await app.ExpectBalancesAsync(today: 100m, all: 80m, reconciled: 100m, @checked: 100m);

        // A month later, the application creates the next occurrence when it loads the database
        app.Time.SetUtcNow(new DateTimeOffset(2026, 2, 12, 12, 0, 0, TimeSpan.Zero));
        await app.RestartAsync();

        await app.OpenAccountAsync("Checking");
        await Expect(app.GetTransactionRow("Internet")).ToHaveCountAsync(2, cancellationToken: XunitCancellationToken);
        await app.ExpectBalancesAsync(today: 80m, all: 60m, reconciled: 100m, @checked: 100m);
    }

    [Fact]
    public async Task ScheduledTransaction_CreateNextOccurrence()
    {
        await using var app = await MoneizApp.StartAsync(Today);
        await app.CreateAccountAsync("Checking", 100m);
        await app.CreateScheduledTransactionAsync("Gym", "Checking", new DateOnly(2026, 2, 1), "FREQ=MONTHLY;BYMONTHDAY=1;INTERVAL=1", -40m);

        var row = app.GetScheduledTransactionRow("Gym");
        await row.GetByRole("button", "Options").Or(row.Locator(".dropdown-toggle")).ClickAsync(cancellationToken: XunitCancellationToken);
        await app.Page.GetByRole("button", "Create next occurrence").ClickAsync(cancellationToken: XunitCancellationToken);

        await app.OpenAccountAsync("Checking");
        await Expect(app.GetTransactionRow("Gym")).ToHaveCountAsync(1, cancellationToken: XunitCancellationToken);
        await app.ExpectBalancesAsync(today: 100m, all: 60m, reconciled: 100m, @checked: 100m);
    }

    [Fact]
    public async Task Reconcile_MarksCheckedTransactionsAsReconciled()
    {
        await using var app = await MoneizApp.StartAsync(Today);
        await app.CreateAccountAsync("Checking", 100m);
        await app.OpenAccountAsync("Checking");
        await app.CreateTransactionAsync("Grocery", -30m, Today.AddDays(-2));
        await app.CreateTransactionAsync("Restaurant", -25m, Today.AddDays(-1));
        await app.CreateTransactionAsync("Pharmacy", -10m, Today);

        await app.CheckTransactionAsync("Grocery");
        await app.CheckTransactionAsync("Restaurant");
        await app.ExpectBalancesAsync(today: 35m, all: 35m, reconciled: 100m, @checked: 45m);

        // Declining the confirmation does not reconcile the transactions
        app.AnswerNextConfirm(false);
        await app.ClickReconcileAsync();
        await app.ExpectBalancesAsync(today: 35m, all: 35m, reconciled: 100m, @checked: 45m);

        app.AnswerNextConfirm(true);
        await app.ClickReconcileAsync();
        Assert.HasCount(2, app.ConfirmMessages);
        Assert.Equal("Balance of 'Checking': USD45.00.\nDo you confirm this balance?", app.ConfirmMessages[1]);

        await app.ExpectBalancesAsync(today: 35m, all: 35m, reconciled: 45m, @checked: 45m);
        await Expect(app.GetTransactionRow("Pharmacy")).ToHaveAttributeAsync("data-transaction-state", "NotChecked", cancellationToken: XunitCancellationToken);

        await app.SetShowReconciledTransactionsAsync(true);
        await Expect(app.GetTransactionRow("Grocery")).ToHaveAttributeAsync("data-transaction-state", "Reconciliated", cancellationToken: XunitCancellationToken);
        await Expect(app.GetTransactionRow("Restaurant")).ToHaveAttributeAsync("data-transaction-state", "Reconciliated", cancellationToken: XunitCancellationToken);

        await app.SetShowReconciledTransactionsAsync(false);
        await Expect(app.TransactionRows).ToHaveCountAsync(1, cancellationToken: XunitCancellationToken);
    }

    [Fact]
    public async Task Data_PersistsAcrossRestart()
    {
        await using var app = await MoneizApp.StartAsync(Today);
        await app.CreateAccountAsync("Checking", 100m);
        await app.OpenAccountAsync("Checking");
        await app.CreateTransactionAsync("Grocery", -30m, Today);

        await app.RestartAsync();

        await app.OpenAccountAsync("Checking");
        await Expect(app.GetTransactionRow("Grocery")).ToHaveCountAsync(1, cancellationToken: XunitCancellationToken);
        await app.ExpectBalancesAsync(today: 70m, all: 70m, reconciled: 100m, @checked: 100m);
    }
}
