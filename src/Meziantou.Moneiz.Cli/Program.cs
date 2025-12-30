#pragma warning disable CA1849 // Call async methods when in an async method
using System.CommandLine;
using Meziantou.Moneiz.Core;

var fileOption = new Option<FileInfo>("--file")
{
    Description = "Path to the database file"
};

var accountsOption = new Option<string[]>("--accounts")
{
    AllowMultipleArgumentsPerToken = true,
    Description = "Account names or IDs to check. If not specified, all accounts will be checked."
};

var daysOption = new Option<int>("--days")
{
    DefaultValueFactory = _ => 3,
    Description = "Number of days to project into the future"
};

var rootCommand = new RootCommand("Check if accounts will be overdraft in the following days");
rootCommand.Options.Add(fileOption);
rootCommand.Options.Add(accountsOption);
rootCommand.Options.Add(daysOption);

rootCommand.SetAction((parseResult) =>
{
    var file = parseResult.GetValue(fileOption);
    if (file is null)
    {
        Console.Error.WriteLine("Error: --file option is required.");
        return Task.FromResult(1);
    }
    
    var accounts = parseResult.GetValue(accountsOption) ?? [];
    var days = parseResult.GetValue(daysOption);
    return CheckOverdraftAsync(file, accounts, days);
});

return rootCommand.Parse(args).Invoke();

static async Task CheckOverdraftAsync(FileInfo file, string[] accountFilter, int days)
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
    
    if (accountFilter.Length > 0)
    {
        accountsToCheck = accountsToCheck
            .Where(a =>
                accountFilter.Any(filter =>
                    a.Name?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                    a.Id.ToString(CultureInfo.InvariantCulture) == filter ||
                    a.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
            )
        ).ToList();

        if (accountsToCheck.Count == 0)
        {
            Console.WriteLine("No accounts found matching the specified filter.");
            return;
        }
    }

    var today = Database.GetToday();
    var targetDate = today.AddDays(days);
    
    Console.WriteLine($"Checking accounts for overdraft from {today:yyyy-MM-dd} to {targetDate:yyyy-MM-dd} ({days} days)");
    Console.WriteLine();

    var hasOverdraft = false;

    foreach (var account in accountsToCheck)
    {
        var currentBalance = db.GetTodayBalance(account);
        var projectedBalance = db.GetBalance(account, targetDate);
        
        var willOverdraft = projectedBalance < 0;
        
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
        Console.WriteLine($"  Projected balance ({targetDate:yyyy-MM-dd}): {FormatAmount(projectedBalance, account.CurrencyIsoCode)}");
        
        if (willOverdraft)
        {
            var deficit = Math.Abs(projectedBalance);
            Console.WriteLine($"  Overdraft amount: {FormatAmount(deficit, account.CurrencyIsoCode)}");
        }
        
        Console.WriteLine();
    }

    if (hasOverdraft)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Warning: One or more accounts will be overdraft!");
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
