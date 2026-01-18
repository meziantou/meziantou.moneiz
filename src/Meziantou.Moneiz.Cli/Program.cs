#pragma warning disable CA1849 // Call async methods when in an async method
using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization;
using Meziantou.Moneiz.Core;

var rootCommand = new RootCommand("Moneiz CLI - Manage your personal finance database");

rootCommand.Subcommands.Add(CreateCheckOverdraftCommand());
rootCommand.Subcommands.Add(CreateAddTransactionCommand());
rootCommand.Subcommands.Add(CreateUpdateTransactionCommand());
rootCommand.Subcommands.Add(CreateGetTransactionsCommand());

return rootCommand.Parse(args).Invoke();

static Command CreateCheckOverdraftCommand()
{
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

    return checkOverdraftCommand;
}

static Command CreateAddTransactionCommand()
{
    var addTransactionCommand = new Command("add-transaction", "Add a new transaction to the database");

    var addFileOption = new Option<FileInfo>("--file")
    {
        Description = "Path to the database file"
    };

    var addAccountIdOption = new Option<int>("--account-id")
    {
        Description = "Account ID for the transaction"
    };

    var toAccountIdOption = new Option<int?>("--to-account-id")
    {
        Description = "Destination account ID for inter-account transfers (optional). Creates a linked transaction."
    };

    var amountOption = new Option<decimal>("--amount")
    {
        Description = "Transaction amount (positive for credit, negative for debit). For inter-account transfers, specify the absolute amount being transferred."
    };

    var valueDateOption = new Option<DateOnly?>("--value-date")
    {
        Description = "Transaction value date (format: yyyy-MM-dd). Defaults to today if not specified."
    };

    var payeeOption = new Option<string?>("--payee")
    {
        Description = "Payee name (optional, not used for inter-account transfers)"
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
    addTransactionCommand.Options.Add(toAccountIdOption);
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
        var toAccountId = parseResult.GetValue(toAccountIdOption);
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

        return await AddTransactionAsync(file, accountId, toAccountId, amount, valueDate, payee, category, comment, isChecked);
    });

    return addTransactionCommand;
}

static Command CreateUpdateTransactionCommand()
{
    var updateTransactionCommand = new Command("update-transaction", "Update an existing transaction in the database");

    var updateFileOption = new Option<FileInfo>("--file")
    {
        Description = "Path to the database file"
    };

    var transactionIdOption = new Option<int>("--transaction-id")
    {
        Description = "Transaction ID to update"
    };

    var amountOption = new Option<decimal?>("--amount")
    {
        Description = "Transaction amount (optional, positive for credit, negative for debit)"
    };

    var valueDateOption = new Option<DateOnly?>("--value-date")
    {
        Description = "Transaction value date (optional, format: yyyy-MM-dd)"
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

    var checkedOption = new Option<bool?>("--checked")
    {
        Description = "Mark transaction as checked or unchecked (optional, true/false)"
    };

    updateTransactionCommand.Options.Add(updateFileOption);
    updateTransactionCommand.Options.Add(transactionIdOption);
    updateTransactionCommand.Options.Add(amountOption);
    updateTransactionCommand.Options.Add(valueDateOption);
    updateTransactionCommand.Options.Add(payeeOption);
    updateTransactionCommand.Options.Add(categoryOption);
    updateTransactionCommand.Options.Add(commentOption);
    updateTransactionCommand.Options.Add(checkedOption);

    updateTransactionCommand.SetAction(async (parseResult) =>
    {
        var file = parseResult.GetValue(updateFileOption);
        var transactionId = parseResult.GetValue(transactionIdOption);
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

        return await UpdateTransactionAsync(file, transactionId, amount, valueDate, payee, category, comment, isChecked);
    });

    return updateTransactionCommand;
}

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

static async Task<int> AddTransactionAsync(FileInfo file, int accountId, int? toAccountId, decimal amount, DateOnly? valueDate, string? payeeName, string? categoryName, string? comment, bool isChecked)
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

    // Get source account
    var account = db.GetAccountById(accountId);
    if (account is null)
    {
        Console.Error.WriteLine($"Error: Account with ID {accountId} not found.");
        return 1;
    }

    // Get destination account for inter-account transfers
    Account? toAccount = null;
    if (toAccountId.HasValue)
    {
        toAccount = db.GetAccountById(toAccountId.Value);
        if (toAccount is null)
        {
            Console.Error.WriteLine($"Error: Destination account with ID {toAccountId.Value} not found.");
            return 1;
        }

        if (toAccountId.Value == accountId)
        {
            Console.Error.WriteLine("Error: Source and destination accounts cannot be the same for inter-account transfers.");
            return 1;
        }
    }

    // Use today's date if not specified
    if (!valueDate.HasValue)
    {
        valueDate = Database.GetToday();
    }

    // For inter-account transfers, payee should not be specified
    Payee? payee = null;
    if (toAccount is null && !string.IsNullOrWhiteSpace(payeeName))
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

    // Create and save transaction(s)
    Transaction transaction;
    Transaction? linkedTransaction = null;

    if (toAccount is not null)
    {
        // Inter-account transfer: create two linked transactions
        // Debit transaction (withdraw from source account)
        transaction = new Transaction
        {
            Account = account,
            Amount = -Math.Abs(amount), // Always negative for the source account
            ValueDate = valueDate.Value,
            Category = category,
            Comment = comment,
            CheckedDate = isChecked ? Database.GetToday() : null
        };

        // Credit transaction (deposit to destination account)
        linkedTransaction = new Transaction
        {
            Account = toAccount,
            Amount = Math.Abs(amount), // Always positive for the destination account
            ValueDate = valueDate.Value,
            Category = category,
            Comment = comment,
            CheckedDate = isChecked ? Database.GetToday() : null
        };

        // Link the transactions
        transaction.LinkedTransaction = linkedTransaction;
        linkedTransaction.LinkedTransaction = transaction;

        db.SaveTransaction(linkedTransaction);
        db.SaveTransaction(transaction);
    }
    else
    {
        // Regular transaction
        transaction = new Transaction
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
    }

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

    if (toAccount is not null)
    {
        // Inter-account transfer
        Console.WriteLine($"  Transfer Amount: {FormatAmount(Math.Abs(amount), account.CurrencyIsoCode)}");
        Console.WriteLine($"  From Account: {account} (Transaction ID: {transaction.Id})");
        Console.WriteLine($"  To Account: {toAccount} (Transaction ID: {linkedTransaction!.Id})");
    }
    else
    {
        // Regular transaction
        Console.WriteLine($"  Transaction ID: {transaction.Id}");
        Console.WriteLine($"  Account: {account}");
        Console.WriteLine($"  Amount: {FormatAmount(amount, account.CurrencyIsoCode)}");
        if (payee is not null)
            Console.WriteLine($"  Payee: {payee}");
    }

    Console.WriteLine($"  Value Date: {valueDate.Value:yyyy-MM-dd}");
    if (category is not null)
        Console.WriteLine($"  Category: {category}");
    if (!string.IsNullOrWhiteSpace(comment))
        Console.WriteLine($"  Comment: {comment}");
    if (isChecked)
        Console.WriteLine($"  Status: Checked");

    return 0;
}

static async Task<int> UpdateTransactionAsync(FileInfo file, int transactionId, decimal? amount, DateOnly? valueDate, string? payeeName, string? categoryName, string? comment, bool? isChecked)
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

    // Get the transaction to update
    var transaction = db.GetTransactionById(transactionId);
    if (transaction is null)
    {
        Console.Error.WriteLine($"Error: Transaction with ID {transactionId} not found.");
        return 1;
    }

    // Check if this is a linked transaction (inter-account transfer)
    var isLinkedTransaction = transaction.LinkedTransaction is not null;

    // Update amount if specified
    if (amount.HasValue)
    {
        if (isLinkedTransaction)
        {
            // For linked transactions, update both transactions
            var linkedTransaction = transaction.LinkedTransaction!;
            var absAmount = Math.Abs(amount.Value);

            // Determine which is debit and which is credit
            if (transaction.Amount < 0)
            {
                transaction.Amount = -absAmount;
                linkedTransaction.Amount = absAmount;
            }
            else
            {
                transaction.Amount = absAmount;
                linkedTransaction.Amount = -absAmount;
            }

            db.SaveTransaction(linkedTransaction);
        }
        else
        {
            transaction.Amount = amount.Value;
        }
    }

    // Update value date if specified
    if (valueDate.HasValue)
    {
        transaction.ValueDate = valueDate.Value;
        if (isLinkedTransaction)
        {
            transaction.LinkedTransaction!.ValueDate = valueDate.Value;
            db.SaveTransaction(transaction.LinkedTransaction!);
        }
    }

    // Update payee if specified (only for non-linked transactions)
    if (payeeName is not null)
    {
        if (isLinkedTransaction)
        {
            Console.Error.WriteLine("Warning: Cannot set payee for inter-account transfers. Payee will be ignored.");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(payeeName))
            {
                transaction.Payee = db.GetOrCreatePayeeByName(payeeName);
            }
            else
            {
                transaction.Payee = null;
            }
        }
    }

    // Update category if specified
    if (categoryName is not null)
    {
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

            var category = db.Categories.FirstOrDefault(c =>
                c.Name == name &&
                string.Equals(c.GroupName, groupName, StringComparison.Ordinal));

            if (category is null)
            {
                category = new Category { Name = name, GroupName = groupName };
                db.SaveCategory(category);
            }

            transaction.Category = category;
            if (isLinkedTransaction)
            {
                transaction.LinkedTransaction!.Category = category;
                db.SaveTransaction(transaction.LinkedTransaction!);
            }
        }
        else
        {
            transaction.Category = null;
            if (isLinkedTransaction)
            {
                transaction.LinkedTransaction!.Category = null;
                db.SaveTransaction(transaction.LinkedTransaction!);
            }
        }
    }

    // Update comment if specified
    if (comment is not null)
    {
        transaction.Comment = comment;
        if (isLinkedTransaction)
        {
            transaction.LinkedTransaction!.Comment = comment;
            db.SaveTransaction(transaction.LinkedTransaction!);
        }
    }

    // Update checked status if specified
    if (isChecked.HasValue)
    {
        if (isChecked.Value)
        {
            transaction.CheckedDate = Database.GetToday();
            if (isLinkedTransaction)
            {
                transaction.LinkedTransaction!.CheckedDate = Database.GetToday();
                db.SaveTransaction(transaction.LinkedTransaction!);
            }
        }
        else
        {
            transaction.CheckedDate = null;
            if (isLinkedTransaction)
            {
                transaction.LinkedTransaction!.CheckedDate = null;
                db.SaveTransaction(transaction.LinkedTransaction!);
            }
        }
    }

    // Save the updated transaction
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
    Console.WriteLine("✓ Transaction updated successfully");
    Console.ResetColor();

    Console.WriteLine($"  Transaction ID: {transaction.Id}");
    Console.WriteLine($"  Account: {transaction.Account}");
    Console.WriteLine($"  Amount: {FormatAmount(transaction.Amount, transaction.Account?.CurrencyIsoCode)}");
    Console.WriteLine($"  Value Date: {transaction.ValueDate:yyyy-MM-dd}");

    if (transaction.Payee is not null)
        Console.WriteLine($"  Payee: {transaction.Payee}");

    if (transaction.Category is not null)
        Console.WriteLine($"  Category: {transaction.Category}");

    if (!string.IsNullOrWhiteSpace(transaction.Comment))
        Console.WriteLine($"  Comment: {transaction.Comment}");

    if (transaction.CheckedDate.HasValue)
        Console.WriteLine($"  Status: Checked");
    else
        Console.WriteLine($"  Status: Not Checked");

    if (isLinkedTransaction)
    {
        Console.WriteLine($"  Linked Transaction ID: {transaction.LinkedTransaction!.Id}");
        Console.WriteLine($"  Linked Account: {transaction.LinkedTransaction!.Account}");
    }

    return 0;
}

static Command CreateGetTransactionsCommand()
{
    var getTransactionsCommand = new Command("get-transactions", "Get the list of transactions from an account (output as JSON)");

    var fileOption = new Option<FileInfo>("--file")
    {
        Description = "Path to the database file"
    };

    var accountIdOption = new Option<int?>("--account-id")
    {
        Description = "Filter by account ID"
    };

    var payeeIdOption = new Option<int?>("--payee-id")
    {
        Description = "Filter by payee ID"
    };

    var categoryIdOption = new Option<int?>("--category-id")
    {
        Description = "Filter by category ID"
    };

    var minAmountOption = new Option<decimal?>("--min-amount")
    {
        Description = "Filter by minimum amount"
    };

    var maxAmountOption = new Option<decimal?>("--max-amount")
    {
        Description = "Filter by maximum amount"
    };

    var fromDateOption = new Option<DateOnly?>("--from-date")
    {
        Description = "Filter by start date (value date, format: yyyy-MM-dd)"
    };

    var toDateOption = new Option<DateOnly?>("--to-date")
    {
        Description = "Filter by end date (value date, format: yyyy-MM-dd)"
    };

    var checkedOption = new Option<bool?>("--checked")
    {
        Description = "Filter by checked status (true/false)"
    };

    var reconciliatedOption = new Option<bool?>("--reconciliated")
    {
        Description = "Filter by reconciliation status (true/false)"
    };

    var linkedTransactionIdOption = new Option<int?>("--linked-transaction-id")
    {
        Description = "Filter by linked transaction ID"
    };

    getTransactionsCommand.Options.Add(fileOption);
    getTransactionsCommand.Options.Add(accountIdOption);
    getTransactionsCommand.Options.Add(payeeIdOption);
    getTransactionsCommand.Options.Add(categoryIdOption);
    getTransactionsCommand.Options.Add(minAmountOption);
    getTransactionsCommand.Options.Add(maxAmountOption);
    getTransactionsCommand.Options.Add(fromDateOption);
    getTransactionsCommand.Options.Add(toDateOption);
    getTransactionsCommand.Options.Add(checkedOption);
    getTransactionsCommand.Options.Add(reconciliatedOption);
    getTransactionsCommand.Options.Add(linkedTransactionIdOption);

    getTransactionsCommand.SetAction(async (parseResult) =>
    {
        var file = parseResult.GetValue(fileOption);
        var accountId = parseResult.GetValue(accountIdOption);
        var payeeId = parseResult.GetValue(payeeIdOption);
        var categoryId = parseResult.GetValue(categoryIdOption);
        var minAmount = parseResult.GetValue(minAmountOption);
        var maxAmount = parseResult.GetValue(maxAmountOption);
        var fromDate = parseResult.GetValue(fromDateOption);
        var toDate = parseResult.GetValue(toDateOption);
        var isChecked = parseResult.GetValue(checkedOption);
        var isReconciliated = parseResult.GetValue(reconciliatedOption);
        var linkedTransactionId = parseResult.GetValue(linkedTransactionIdOption);

        if (file is null)
        {
            Console.Error.WriteLine("Error: --file option is required.");
            return 1;
        }

        return await GetTransactionsAsync(
            file,
            accountId,
            payeeId,
            categoryId,
            minAmount,
            maxAmount,
            fromDate,
            toDate,
            isChecked,
            isReconciliated,
            linkedTransactionId);
    });

    return getTransactionsCommand;
}

static async Task<int> GetTransactionsAsync(
    FileInfo file,
    int? accountId,
    int? payeeId,
    int? categoryId,
    decimal? minAmount,
    decimal? maxAmount,
    DateOnly? fromDate,
    DateOnly? toDate,
    bool? isChecked,
    bool? isReconciliated,
    int? linkedTransactionId)
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

    // Start with all transactions
    IEnumerable<Transaction> transactions = db.Transactions;

    // Apply filters
    if (accountId.HasValue)
    {
        transactions = transactions.Where(t => t.AccountId == accountId.Value);
    }

    if (payeeId.HasValue)
    {
        transactions = transactions.Where(t => t.PayeeId == payeeId.Value);
    }

    if (categoryId.HasValue)
    {
        transactions = transactions.Where(t => t.CategoryId == categoryId.Value);
    }

    if (minAmount.HasValue)
    {
        transactions = transactions.Where(t => t.Amount >= minAmount.Value);
    }

    if (maxAmount.HasValue)
    {
        transactions = transactions.Where(t => t.Amount <= maxAmount.Value);
    }

    if (fromDate.HasValue)
    {
        transactions = transactions.Where(t => t.ValueDate >= fromDate.Value);
    }

    if (toDate.HasValue)
    {
        transactions = transactions.Where(t => t.ValueDate <= toDate.Value);
    }

    if (isChecked.HasValue)
    {
        if (isChecked.Value)
        {
            transactions = transactions.Where(t => t.CheckedDate.HasValue);
        }
        else
        {
            transactions = transactions.Where(t => !t.CheckedDate.HasValue);
        }
    }

    if (isReconciliated.HasValue)
    {
        if (isReconciliated.Value)
        {
            transactions = transactions.Where(t => t.ReconciliationDate.HasValue);
        }
        else
        {
            transactions = transactions.Where(t => !t.ReconciliationDate.HasValue);
        }
    }

    if (linkedTransactionId.HasValue)
    {
        transactions = transactions.Where(t => t.LinkedTransactionId == linkedTransactionId.Value);
    }

    // Convert to list for serialization
    var transactionsList = transactions.ToList();

    // Create a DTO for JSON serialization that includes readable information
    var transactionsOutput = transactionsList.Select(t => new
    {
        Id = t.Id,
        Amount = t.Amount,
        Comment = t.Comment,
        ValueDate = t.ValueDate,
        CheckedDate = t.CheckedDate,
        ReconciliationDate = t.ReconciliationDate,
        AccountId = t.AccountId,
        AccountName = t.Account?.Name,
        PayeeId = t.PayeeId,
        PayeeName = t.Payee?.Name,
        CategoryId = t.CategoryId,
        CategoryName = t.Category?.ToString(),
        LinkedTransactionId = t.LinkedTransactionId,
        State = t.State.ToString()
    }).ToList();

    // Serialize to JSON
    var jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    var json = JsonSerializer.Serialize(transactionsOutput, jsonOptions);
    Console.WriteLine(json);

    return 0;
}
