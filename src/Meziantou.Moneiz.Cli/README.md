# Moneiz CLI

Command-line interface for managing Moneiz personal finance database files.

## Installation

Install as a global .NET tool:

```bash
dotnet tool install --global Meziantou.Moneiz.Cli
```

Or run directly from source:

```bash
dotnet run --project src/Meziantou.Moneiz.Cli/Meziantou.Moneiz.Cli.csproj -- [command] [options]
```

## Commands

### get-transactions

Get the list of transactions from the database with optional filters. Output is in JSON format.

#### Usage

```bash
meziantou.moneiz get-transactions [options]
```

#### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--file <file>` | Yes | Path to the database file |
| `--account-id <id>` | No | Filter by account ID |
| `--payee-id <id>` | No | Filter by payee ID |
| `--category-id <id>` | No | Filter by category ID |
| `--min-amount <amount>` | No | Filter by minimum amount |
| `--max-amount <amount>` | No | Filter by maximum amount |
| `--from-date <date>` | No | Filter by start date (value date, format: yyyy-MM-dd) |
| `--to-date <date>` | No | Filter by end date (value date, format: yyyy-MM-dd) |
| `--checked <bool>` | No | Filter by checked status (true/false) |
| `--reconciliated <bool>` | No | Filter by reconciliation status (true/false) |
| `--linked-transaction-id <id>` | No | Filter by linked transaction ID |

#### Examples

**Get all transactions:**

```bash
meziantou.moneiz get-transactions --file ~/finances.moneiz
```

**Get transactions for a specific account:**

```bash
meziantou.moneiz get-transactions \
  --file ~/finances.moneiz \
  --account-id 1
```

**Get transactions within a date range:**

```bash
meziantou.moneiz get-transactions \
  --file ~/finances.moneiz \
  --from-date 2026-01-01 \
  --to-date 2026-01-31
```

**Get unchecked transactions:**

```bash
meziantou.moneiz get-transactions \
  --file ~/finances.moneiz \
  --checked false
```

**Get expenses (negative amounts) for a specific payee:**

```bash
meziantou.moneiz get-transactions \
  --file ~/finances.moneiz \
  --payee-id 5 \
  --max-amount 0
```

**Combine multiple filters:**

```bash
meziantou.moneiz get-transactions \
  --file ~/finances.moneiz \
  --account-id 1 \
  --category-id 3 \
  --from-date 2026-01-01 \
  --checked true
```

#### Output Format

The command outputs transactions in JSON format with the following structure:

```json
[
  {
    "Id": 1,
    "Amount": -50.00,
    "Comment": "Weekly groceries",
    "ValueDate": "2026-01-12",
    "CheckedDate": "2026-01-17",
    "AccountId": 1,
    "AccountName": "Checking Account",
    "PayeeId": 1,
    "PayeeName": "Supermarket",
    "CategoryId": 1,
    "CategoryName": "Food::Groceries",
    "State": "Checked"
  }
]
```

Fields are omitted from the output if they are null.

### add-transaction

Add a new transaction to the database. Supports both regular transactions and inter-account transfers.

#### Usage

```bash
meziantou.moneiz add-transaction [options]
```

#### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--file <file>` | Yes | Path to the database file |
| `--account-id <id>` | Yes | Account ID for the transaction |
| `--amount <amount>` | Yes | Transaction amount (positive for credit, negative for debit) |
| `--to-account-id <id>` | No | Destination account ID for inter-account transfers |
| `--value-date <date>` | No | Transaction date (format: yyyy-MM-dd). Defaults to today |
| `--payee <name>` | No | Payee name (not used for inter-account transfers) |
| `--category <name>` | No | Category name (format: 'GroupName::CategoryName' or 'CategoryName') |
| `--comment <text>` | No | Transaction comment |
| `--checked` | No | Mark transaction as checked |

#### Examples

**Add a regular expense transaction:**

```bash
meziantou.moneiz add-transaction \
  --file ~/finances.moneiz \
  --account-id 1 \
  --amount -50.00 \
  --payee "Coffee Shop" \
  --category "Food::Dining" \
  --comment "Morning coffee"
```

**Add an income transaction:**

```bash
meziantou.moneiz add-transaction \
  --file ~/finances.moneiz \
  --account-id 1 \
  --amount 2500.00 \
  --payee "Employer" \
  --category "Income::Salary" \
  --value-date 2026-01-15 \
  --checked
```

**Transfer money between accounts:**

```bash
meziantou.moneiz add-transaction \
  --file ~/finances.moneiz \
  --account-id 1 \
  --to-account-id 2 \
  --amount 200 \
  --comment "Transfer to savings"
```

This creates two linked transactions:
- A debit of -200 from account 1
- A credit of +200 to account 2

**Add a transaction with grouped category:**

```bash
meziantou.moneiz add-transaction \
  --file ~/finances.moneiz \
  --account-id 1 \
  --amount -75.50 \
  --payee "Grocery Store" \
  --category "Shopping::Groceries"
```

### check-overdraft

Check if accounts will fall below their minimum balance thresholds in the coming days.

#### Usage

```bash
meziantou.moneiz check-overdraft [options]
```

#### Options

| Option | Required | Description |
|--------|----------|-------------|
| `--file <file>` | Yes | Path to the database file |
| `--account-id <ids>` | No | Account IDs to check (comma-separated, e.g., '1,2,3'). If not specified, checks all accounts with notifications enabled |

#### Examples

**Check all accounts with overdraft notifications enabled:**

```bash
meziantou.moneiz check-overdraft --file ~/finances.moneiz
```

**Check specific accounts:**

```bash
meziantou.moneiz check-overdraft \
  --file ~/finances.moneiz \
  --account-id 1,3,5
```

The command will:
- Process scheduled transactions
- Check balance projections for the configured notification period
- Report accounts that will fall below their minimum balance
- Exit with code 1 if any account will be in overdraft, 0 otherwise

## Notes

### Categories

Categories can be specified in two formats:
- Simple: `"Food"` (no group)
- Grouped: `"Expenses::Food"` (with group name)

Categories are automatically created if they don't exist in the database.

### Payees

Payees are automatically created if they don't exist in the database.

### Inter-Account Transfers

When using `--to-account-id`, the tool creates two linked transactions representing a transfer between accounts. The `--payee` option is ignored for transfers since the linked transaction represents the movement of funds.

### Date Format

All dates must be in ISO 8601 format: `yyyy-MM-dd` (e.g., `2026-01-17`)

## Exit Codes

- `0`: Success
- `1`: Error (missing file, invalid account, overdraft detected, etc.)

## Getting Account IDs

Account IDs are assigned by the Moneiz application when accounts are created. You can view account details in the main Moneiz web application to find the account IDs you need for CLI commands.
