using System.Globalization;
using System.Reflection;
using Meziantou.AspNetCore.Testing;
using Meziantou.Framework.WebPlatform.Dom;
using Meziantou.Framework.WebPlatform.Wasmtime;
using Microsoft.Extensions.Time.Testing;
using static Meziantou.AspNetCore.Testing.Expectations;

namespace Meziantou.Moneiz.E2ETests;

/// <summary>
/// The published Moneiz application, running in the browser engine of Meziantou.Framework.WebPlatform:
/// the .NET WebAssembly runtime runs in Wasmtime, the data is stored by localforage in an in-memory IndexedDB.
/// </summary>
internal sealed class MoneizApp : IAsyncDisposable
{
    private static readonly Uri BaseAddress = new("https://localhost/");
    private static readonly TimeSpan BootTimeout = TimeSpan.FromSeconds(120);
    private static readonly string AppPath = typeof(MoneizApp).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "MoneizAppPath").Value!;

    private readonly WasmtimeRuntime _wasmRuntime = new(new WasmtimeRuntimeOptions());
    private readonly InMemoryIndexedDBBackend _indexedDB = new();
    private readonly Queue<bool> _confirmAnswers = new();
    private readonly WebAppBrowser _browser;
    private TestPage? _page;

    private MoneizApp(DateOnly today)
    {
        Time = new FakeTimeProvider(new DateTimeOffset(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));
        Time.SetLocalTimeZone(TimeZoneInfo.Utc);
        // The application uses timers (setTimeout with a delay), which never fire if the clock is frozen
        Time.AutoAdvanceAmount = TimeSpan.FromMilliseconds(1);

        // The routes of the application (no file extension) serve index.html, as the static web app does
        var loader = TestDocumentLoaders.FromDirectories(BaseAddress, [new("/", AppPath)], fallbackPath: "/index.html");
        _browser = new WebAppBrowser(BaseAddress, loader, new WebAppBrowserOptions
        {
            // The clock of the timers and of the dates: DateTime.Now in the application is the date of the test
            TimeProvider = Time,
            WebAssemblyRuntime = _wasmRuntime,
            DefaultTimeout = TimeSpan.FromSeconds(30),
            ConfigureBrowsingContext = options => options with
            {
                // Shared by the pages, so the data survives a restart of the application
                IndexedDB = _indexedDB,
                UserPromptHandler = OnUserPrompt,
            },
        });
    }

    public FakeTimeProvider Time { get; }

    public TestPage Page => _page ?? throw new InvalidOperationException("The application is not started");

    /// <summary>Gets the messages of the confirm() dialogs, in the order they were shown.</summary>
    public List<string> ConfirmMessages { get; } = [];

    public static async Task<MoneizApp> StartAsync(DateOnly today)
    {
        var app = new MoneizApp(today);
        try
        {
            await app.OpenAsync();
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    /// <summary>Closes the page and opens the application again, as a reload of the browser: the data stays in IndexedDB.</summary>
    public async Task RestartAsync()
    {
        if (_page is not null)
        {
            await _page.DisposeAsync();
            _page = null;
        }

        await OpenAsync();
    }

    /// <summary>Answers the next confirm() dialog.</summary>
    public void AnswerNextConfirm(bool accept) => _confirmAnswers.Enqueue(accept);

    public async Task GoToAccountsAsync()
    {
        await Page.GetByRole("link", "My accounts").ClickAsync(cancellationToken: XunitCancellationToken);
        await Expect(Page.GetByRole("heading", "Accounts")).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    public async Task GoToSchedulerAsync()
    {
        await Page.GetByRole("link", "My scheduler").ClickAsync(cancellationToken: XunitCancellationToken);
        await Expect(Page.GetByRole("heading", "Scheduler")).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    public async Task CreateAccountAsync(string name, decimal initialBalance, string currency = "USD")
    {
        await GoToAccountsAsync();
        await Page.GetByRole("link", "Create an account").ClickAsync(cancellationToken: XunitCancellationToken);
        await Page.GetByLabel("Name", exact: true).FillAsync(name, XunitCancellationToken);
        await Page.GetByLabel("Currency", exact: true).SelectOptionAsync(currency);
        await Page.GetByLabel("Initial balance", exact: true).FillAsync(FormatNumber(initialBalance), XunitCancellationToken);
        await SubmitAsync();
        await Expect(Page.GetByRole("heading", "Accounts")).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
        await Expect(GetAccountRow(name)).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    public Locator GetAccountRow(string name) => Page.Locator("tr[data-account-id]").Filter(new LocatorFilterOptions { HasText = name });

    /// <summary>Opens the transactions of an account from the sidebar.</summary>
    public async Task OpenAccountAsync(string name)
    {
        await Page.Locator(".widget-accounts").GetByRole("link", name, new GetByRoleOptions { Exact = true }).ClickAsync(cancellationToken: XunitCancellationToken);
        await Expect(Page.GetByRole("heading", "Transactions of " + name)).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    /// <summary>Creates a transaction from the transactions page of the current account.</summary>
    public async Task CreateTransactionAsync(string payee, decimal amount, DateOnly date, string? comment = null)
    {
        var heading = Page.Locator("h1").TextContent;
        await Page.GetByRole("link", "Create transaction").ClickAsync(cancellationToken: XunitCancellationToken);
        await Expect(Page.GetByRole("heading", "Create transaction")).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
        await Page.GetByLabel("Payee", exact: true).FillAsync(payee, XunitCancellationToken);
        await Page.GetByLabel("Operation date", exact: true).FillAsync(FormatDate(date), XunitCancellationToken);
        await Page.GetByLabel("Amount", exact: true).FillAsync(FormatNumber(amount), XunitCancellationToken);
        if (comment is not null)
        {
            await Page.GetByLabel("Comment", exact: true).FillAsync(comment, XunitCancellationToken);
        }

        await SubmitAsync();
        await Expect(Page.GetByRole("heading", heading!)).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    /// <summary>Creates a transfer between two accounts from the transactions page of the debited account.</summary>
    public async Task CreateTransferAsync(string debitedAccount, string creditedAccount, decimal amount, DateOnly date)
    {
        await OpenAccountAsync(debitedAccount);
        await Page.GetByRole("link", "Create transaction").ClickAsync(cancellationToken: XunitCancellationToken);
        await Page.GetByLabel("Inter-account", exact: true).CheckAsync(XunitCancellationToken);
        await SelectAccountAsync("Debited account", debitedAccount);
        await SelectAccountAsync("CreditedAccount", creditedAccount);
        await Page.GetByLabel("Operation date", exact: true).FillAsync(FormatDate(date), XunitCancellationToken);
        await Page.GetByLabel("Amount", exact: true).FillAsync(FormatNumber(amount), XunitCancellationToken);
        await SubmitAsync();
        await Expect(Page.GetByRole("heading", "Transactions of " + debitedAccount)).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    public async Task CreateScheduledTransactionAsync(string name, string account, DateOnly startDate, string recurrenceRule, decimal amount)
    {
        await GoToSchedulerAsync();
        await Page.GetByRole("link", "Create scheduled transaction").ClickAsync(cancellationToken: XunitCancellationToken);
        await Page.GetByLabel("Name", exact: true).FillAsync(name, XunitCancellationToken);
        await Page.GetByLabel("Start date", exact: true).FillAsync(FormatDate(startDate), XunitCancellationToken);
        await Page.GetByRole("combobox", "Recurrence rule (generator)", new GetByRoleOptions { Exact = true }).FillAsync(recurrenceRule, XunitCancellationToken);
        await SelectAccountAsync("Account", account);
        await Page.GetByLabel("Payee", exact: true).FillAsync(name, XunitCancellationToken);
        await Page.GetByLabel("Amount", exact: true).FillAsync(FormatNumber(amount), XunitCancellationToken);
        await SubmitAsync();
        await Expect(Page.GetByRole("heading", "Scheduler")).ToBeVisibleAsync(cancellationToken: XunitCancellationToken);
    }

    public Locator GetScheduledTransactionRow(string name) => Page.Locator("tr[data-scheduled-transaction-id]").Filter(new LocatorFilterOptions { HasText = name });

    /// <summary>Gets the rows of the transactions of the current page.</summary>
    public Locator TransactionRows => Page.Locator("tr[data-transaction-id]");

    public Locator GetTransactionRow(string text) => TransactionRows.Filter(new LocatorFilterOptions { HasText = text });

    /// <summary>Gets the amount of a balance of the current account: today, all, reconciled or checked.</summary>
    public Locator GetBalance(string name) => Page.GetByTestId("balance-" + name).Locator(".amount-value");

    public async Task ExpectBalancesAsync(decimal today, decimal all, decimal reconciled, decimal @checked)
    {
        await Expect(GetBalance("today")).ToHaveTextAsync(FormatAmount(today), cancellationToken: XunitCancellationToken);
        await Expect(GetBalance("all")).ToHaveTextAsync(FormatAmount(all), cancellationToken: XunitCancellationToken);
        await Expect(GetBalance("reconciled")).ToHaveTextAsync(FormatAmount(reconciled), cancellationToken: XunitCancellationToken);
        await Expect(GetBalance("checked")).ToHaveTextAsync(FormatAmount(@checked), cancellationToken: XunitCancellationToken);
    }

    /// <summary>Marks a transaction of the current account as checked.</summary>
    public async Task CheckTransactionAsync(string text)
    {
        var row = GetTransactionRow(text);
        await row.GetByRole("checkbox", "Checked").ClickAsync(cancellationToken: XunitCancellationToken);
        await Expect(row).ToHaveAttributeAsync("data-transaction-state", "Checked", cancellationToken: XunitCancellationToken);
    }

    public async Task ClickReconcileAsync()
    {
        await Page.GetByRole("button", "Reconcile").ClickAsync(cancellationToken: XunitCancellationToken);
        await Page.WaitForIdleAsync(cancellationToken: XunitCancellationToken);
    }

    public async Task SetShowReconciledTransactionsAsync(bool show)
    {
        await Page.GetByLabel("Show reconciled transactions", exact: true).SetCheckedAsync(show, XunitCancellationToken);
    }

    public static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.DisposeAsync();
        }

        await _browser.DisposeAsync();
        _wasmRuntime.Dispose();
    }

    private async Task OpenAsync()
    {
        _page = await _browser.OpenAsync("/", XunitCancellationToken);
        await Expect(_page.GetByRole("link", "My accounts")).ToBeVisibleAsync(BootTimeout, XunitCancellationToken);
    }

    private async Task SelectAccountAsync(string label, string account)
    {
        var control = Page.GetByLabel(label, exact: true);
        await control.ClickAsync(cancellationToken: XunitCancellationToken);
        await Page.GetByRole("option", account, new GetByRoleOptions { Exact = true }).ClickAsync(cancellationToken: XunitCancellationToken);
        await Expect(control).ToContainTextAsync(account, cancellationToken: XunitCancellationToken);
    }

    private async Task SubmitAsync()
    {
        await Page.GetByRole("button", "Submit").ClickAsync(cancellationToken: XunitCancellationToken);
    }

    private UserPromptResult OnUserPrompt(CallbackContext context, UserPrompt prompt)
    {
        if (prompt.Type is UserPromptType.Confirm)
        {
            ConfirmMessages.Add(prompt.Message);
            var accept = !_confirmAnswers.TryDequeue(out var answer) || answer;
            return accept ? UserPromptResult.Accept(null) : UserPromptResult.Dismiss;
        }

        return UserPromptResult.Accept(null);
    }

    private static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatNumber(decimal value) => value.ToString(CultureInfo.InvariantCulture);
}
