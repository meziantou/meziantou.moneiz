#pragma warning disable CA1849 // Call async methods when in an async method
using System.CommandLine;
using Meziantou.Moneiz.Core;

var rootCommand = new RootCommand("Moneiz CLI - Manage your personal finance database");

// Check Overdraft Command
var checkOverdraftCommand = new Command("check-overdraft", "Check if accounts will be overdraft in the following days");

var checkFileOption = new Option<FileInfo>("--file")
{
    Description = "Path to the database file"
};

var accountIdsOption = new Option<string>("--account-id")
{
    Description = "Account IDs to check (comma-separated, e.g., '1,2,3'). If not specified, all accounts with notifications enabled will be checked."
};

checkOverdraftCommand.Options.Add(checkFileOption);
checkOverdraftCommand.Options.Add(accountIdsOption);

checkOverdraftCommand.SetAction((parseResult) =>
{
    var file = parseResult.GetValue(checkFileOption);
    if (file is null)
    {
        Console.Error.WriteLine("Error: --file option is required.");
        return Task.FromResult(1);
    }

    var accountIds = parseResult.GetValue(accountIdsOption);
    return CheckOverdraftAsync(file, accountIds);
});

rootCommand.Subcommands.Add(checkOverdraftCommand);

// Add Transaction Command
var addTransactionCommand = new Command("add-transaction", "Add a new transaction to the database");

var addFileOption = new Option<FileInfo>("--file")
{
    Description = "Path to the database file"
};

var addAccountIdOption = new Option<int>("--account-id")
{
    Description = "Account ID for the transaction"
};

var amountOption = new Option<decimal>("--amount")
{
    Description = "Transaction amount (positive for credit, negative for debit)"
};

var valueDateOption = new Option<DateOnly?>("--value-date")
{
    Description = "Transaction value date (format: yyyy-MM-dd). Defaults to today if not specified."
};

var payeeOption = new Option<string?>("--payee")
{
    Description = "Payee name (optional)"
};

var categoryOption = new Option<string?>("--category")
{
    Description = "Category name (optional, format: 'GroupName::CategoryName' or just 'CategoryName')"
};

var commentOption = new Option<string?>("--comment")
{
    Description = "Transaction comment (optional)"
};

var checkedOption = new Option<bool>("--checked")
{
    Description = "Mark transaction as checked (optional, default: false)"
};

addTransactionCommand.Options.Add(addFileOption);
addTransactionCommand.Options.Add(addAccountIdOption);
addTransactionCommand.Options.Add(amountOption);
addTransactionCommand.Options.Add(valueDateOption);
addTransactionCommand.Options.Add(payeeOption);
addTransactionCommand.Options.Add(categoryOption);
addTransactionCommand.Options.Add(commentOption);
addTransactionCommand.Options.Add(checkedOption);

addTransactionCommand.SetAction(async (parseResult) =>
{
    var file = parseResult.GetValue(addFileOption);
    var accountId = parseResult.GetValue(addAccountIdOption);
    var amount = parseResult.GetValue(amountOption);
    var valueDate = parseResult.GetValue(valueDateOption);
    var payee = parseResult.GetValue(payeeOption);
    var category = parseResult.GetValue(categoryOption);
    var comment = parseResult.GetValue(commentOption);
    var isChecked = parseResult.GetValue(checkedOption);

    if (file is null)
    {
        Console.Error.WriteLine("Error: --file option is required.");
        return 1;
    }

    return await AddTransactionAsync(file, accountId, amount, valueDate, payee, category, comment, isChecked);
});

rootCommand.Subcommands.Add(addTransactionCommand);

return rootCommand.Parse(args).Invoke();

static async Task CheckOverdraftAsync(FileInfo file, string? accountIdFilter)
{
    if (!file.Exists)
    {
        Console.Error.WriteLine($"Error: Database file '{file.FullName}' not found.");
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

    var accountsToCheck = db.VisibleAccounts
        .Where(a => a.OverdraftNotificationEnabled)
        .ToList();

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
            Console.Error.WriteLine($"Error: Account ID(s) not found or notification not enabled: {string.Join(", ", missingIds)}");
            Environment.Exit(1);
        }
    }

    if (accountsToCheck.Count == 0)
    {
        Console.WriteLine("No accounts with overdraft notifications enabled.");
        return;
    }

    var today = Database.GetToday();

    Console.WriteLine("Checking accounts for overdraft using their configured notification settings");
    Console.WriteLine();

    var hasOverdraft = false;

    foreach (var account in accountsToCheck)
    {
        var days = account.OverdraftNotificationCheckPeriodDays;
        var minimumBalance = account.OverdraftNotificationAmount;
        var targetDate = today.AddDays(days);

        // Process scheduled transactions up to the target date for this account
        var tempDb = db;
        tempDb.ProcessScheduledTransactions(days);

        var currentBalance = tempDb.GetTodayBalance(account);

        // Check balance for each day in the period
        DateOnly? overdraftDate = null;
        decimal? lowestBalance = null;

        for (var i = 0; i <= days; i++)
        {
            var checkDate = today.AddDays(i);
            var balance = tempDb.GetBalance(account, checkDate);

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

        Console.WriteLine($"  Check period: {days} days (until {targetDate:yyyy-MM-dd})");
        Console.WriteLine($"  Minimum balance threshold: {FormatAmount(minimumBalance, account.CurrencyIsoCode)}");
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
            var projectedBalance = tempDb.GetBalance(account, targetDate);
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

static async Task<int> AddTransactionAsync(FileInfo file, int accountId, decimal amount, DateOnly? valueDate, string? payeeName, string? categoryName, string? comment, bool isChecked)
{
    if (!file.Exists)
    {
        Console.Error.WriteLine($"Error: Database file '{file.FullName}' not found.");
        return 1;
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
        return 1;
    }

    // Get account
    var account = db.GetAccountById(accountId);
    if (account is null)
    {
        Console.Error.WriteLine($"Error: Account with ID {accountId} not found.");
        return 1;
    }

    // Use today's date if not specified
    if (!valueDate.HasValue)
    {
        valueDate = Database.GetToday();
    }

    // Get or create payee if specified
    Payee? payee = null;
    if (!string.IsNullOrWhiteSpace(payeeName))
    {
        payee = db.GetOrCreatePayeeByName(payeeName);
    }

    // Parse and get category if specified
    Category? category = null;
    if (!string.IsNullOrWhiteSpace(categoryName))
    {
        string? groupName = null;
        string? name = categoryName;

        if (categoryName.Contains("::", StringComparison.Ordinal))
        {
            var parts = categoryName.Split("::", 2);
            groupName = parts[0];
            name = parts[1];
        }

        category = db.Categories.FirstOrDefault(c =>
            c.Name == name &&
            string.Equals(c.GroupName, groupName, StringComparison.Ordinal));

        if (category is null)
        {
            category = new Category { Name = name, GroupName = groupName };
            db.SaveCategory(category);
        }
    }

    // Create and save transaction
    var transaction = new Transaction
    {
        Account = account,
        Amount = amount,
        ValueDate = valueDate.Value,
        Payee = payee,
        Category = category,
        Comment = comment,
        CheckedDate = isChecked ? Database.GetToday() : null
    };

    db.SaveTransaction(transaction);

    // Save database back to file
    try
    {
        var exportedData = db.Export();
        await File.WriteAllBytesAsync(file.FullName, exportedData);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error saving database: {ex.Message}");
        return 1;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("✓ Transaction added successfully");
    Console.ResetColor();
    Console.WriteLine($"  Transaction ID: {transaction.Id}");
    Console.WriteLine($"  Account: {account}");
    Console.WriteLine($"  Amount: {FormatAmount(amount, account.CurrencyIsoCode)}");
    Console.WriteLine($"  Value Date: {valueDate.Value:yyyy-MM-dd}");
    if (payee is not null)
        Console.WriteLine($"  Payee: {payee}");
    if (category is not null)
        Console.WriteLine($"  Category: {category}");
    if (!string.IsNullOrWhiteSpace(comment))
        Console.WriteLine($"  Comment: {comment}");
    if (isChecked)
        Console.WriteLine($"  Status: Checked");

    return 0;
}
