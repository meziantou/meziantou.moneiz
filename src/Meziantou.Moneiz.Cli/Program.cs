#pragma warning disable CA1849 // Call async methods when in an async method
using System.CommandLine;
using Meziantou.Moneiz.Core;

var fileOption = new Option<FileInfo>("--file")
{
    Description = "Path to the database file"
};

var accountIdsOption = new Option<string>("--account-id")
{
    Description = "Account IDs to check (comma-separated, e.g., '1,2,3'). If not specified, all accounts will be checked."
};

var daysOption = new Option<int>("--days")
{
    DefaultValueFactory = _ => 3,
    Description = "Number of days to project into the future"
};

var minimumBalanceOption = new Option<decimal>("--minimum-balance")
{
    DefaultValueFactory = _ => 0,
    Description = "Minimum balance threshold. Accounts below this balance will be flagged."
};

var rootCommand = new RootCommand("Check if accounts will be overdraft in the following days");
rootCommand.Options.Add(fileOption);
rootCommand.Options.Add(accountIdsOption);
rootCommand.Options.Add(daysOption);
rootCommand.Options.Add(minimumBalanceOption);

rootCommand.SetAction((parseResult) =>
{
    var file = parseResult.GetValue(fileOption);
    if (file is null)
    {
        Console.Error.WriteLine("Error: --file option is required.");
        return Task.FromResult(1);
    }

    var accountIds = parseResult.GetValue(accountIdsOption);
    var days = parseResult.GetValue(daysOption);
    var minimumBalance = parseResult.GetValue(minimumBalanceOption);
    return CheckOverdraftAsync(file, accountIds, days, minimumBalance);
});

return rootCommand.Parse(args).Invoke();

static async Task CheckOverdraftAsync(FileInfo file, string? accountIdFilter, int days, decimal minimumBalance)
{
    if (!file.Exists)
    {
        Console.Error.WriteLine($"Error: Database file '{file.FullName}' not found.");
        Environment.Exit(1);
    }

    if (days < 0)
    {
        Console.Error.WriteLine($"Error: Days must be a positive number.");
        Environment.Exit(1);
    }

    Database db;
    try
    {
        await using var stream = file.OpenRead();
        db = await Database.Load(stream);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error loading database: {ex.Message}");
        Environment.Exit(1);
        return;
    }

    var accountsToCheck = db.VisibleAccounts.ToList();

    if (!string.IsNullOrWhiteSpace(accountIdFilter))
    {
        var accountIds = new HashSet<int>();
        var idStrings = accountIdFilter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var id in idStrings)
        {
            if (int.TryParse(id, CultureInfo.InvariantCulture, out var accountId))
            {
                accountIds.Add(accountId);
            }
            else
            {
                Console.Error.WriteLine($"Warning: '{id}' is not a valid account ID and will be ignored.");
            }
        }

        accountsToCheck = accountsToCheck.Where(a => accountIds.Contains(a.Id)).ToList();

        var foundIds = new HashSet<int>(accountsToCheck.Select(a => a.Id));
        var missingIds = accountIds.Where(id => !foundIds.Contains(id)).ToList();

        if (missingIds.Count > 0)
        {
            Console.Error.WriteLine($"Error: Account ID(s) not found: {string.Join(", ", missingIds)}");
            Environment.Exit(1);
        }
    }

    var today = Database.GetToday();
    var targetDate = today.AddDays(days);

    // Process scheduled transactions up to the target date
    db.ProcessScheduledTransactions(days);

    Console.WriteLine($"Checking accounts for overdraft from {today:yyyy-MM-dd} to {targetDate:yyyy-MM-dd} ({days} days)");
    Console.WriteLine($"Minimum balance threshold: {minimumBalance:N2}");
    Console.WriteLine();

    var hasOverdraft = false;

    foreach (var account in accountsToCheck)
    {
        var currentBalance = db.GetTodayBalance(account);

        // Check balance for each day in the period
        DateOnly? overdraftDate = null;
        decimal? lowestBalance = null;

        for (var i = 0; i <= days; i++)
        {
            var checkDate = today.AddDays(i);
            var balance = db.GetBalance(account, checkDate);

            if (balance < minimumBalance)
            {
                if (!overdraftDate.HasValue || balance < lowestBalance)
                {
                    overdraftDate = checkDate;
                    lowestBalance = balance;
                }
            }
        }

        var willOverdraft = overdraftDate.HasValue;

        if (willOverdraft)
        {
            hasOverdraft = true;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"⚠ {account}");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {account}");
            Console.ResetColor();
        }

        Console.WriteLine($"  Current balance: {FormatAmount(currentBalance, account.CurrencyIsoCode)}");

        if (willOverdraft && overdraftDate.HasValue && lowestBalance.HasValue)
        {
            Console.WriteLine($"  Will fall below minimum on: {overdraftDate.Value:yyyy-MM-dd}");
            Console.WriteLine($"  Lowest balance: {FormatAmount(lowestBalance.Value, account.CurrencyIsoCode)}");
            var deficit = minimumBalance - lowestBalance.Value;
            Console.WriteLine($"  Below minimum by: {FormatAmount(deficit, account.CurrencyIsoCode)}");
        }
        else
        {
            var projectedBalance = db.GetBalance(account, targetDate);
            Console.WriteLine($"  Projected balance ({targetDate:yyyy-MM-dd}): {FormatAmount(projectedBalance, account.CurrencyIsoCode)}");
        }

        Console.WriteLine();
    }

    if (hasOverdraft)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Warning: One or more accounts will fall below the minimum balance!");
        Console.ResetColor();
        Environment.Exit(1);
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("All accounts are within acceptable balance.");
        Console.ResetColor();
    }
}

static string FormatAmount(decimal amount, string? currencyCode)
{
    var sign = amount >= 0 ? "" : "-";
    var absAmount = Math.Abs(amount);
    return $"{sign}{absAmount:N2} {currencyCode ?? ""}".Trim();
}
