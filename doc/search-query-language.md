# Search query language

This page documents the search query language for each search box in the app.

## Transactions search box

Applies to:

- `/transactions`
- `/accounts/{id}/transactions`
- `/categories/{id}/transactions`
- `/category-group/{group}/transactions`
- `/payees/{id}/transactions`

### Syntax

- **Text query**: `groceries`
- **Field query**: `field:value`
- **Comparison operators**: `:`, `=`, `<>`, `<`, `<=`, `>`, `>=`
- **Boolean operators**: `AND`, `OR`, `NOT`
- **Grouping**: parentheses `(...)`
- **Quoted values**: `"..."` for values with spaces

### Supported fields

| Field | Type | Description |
| --- | --- | --- |
| `bank` | text | Account bank name |
| `account` | text | Account name |
| `accountId` | number | Account id |
| `title` | text | Transaction title |
| `payee` | text | Payee name |
| `category` | text | Category name |
| `categoryGroup` | text | Category group name |
| `comment` | text | Transaction comment |
| `state` | enum | `NotChecked`, `Checked`, `Reconciliated` |
| `date` | date | Transaction date (`yyyy-MM-dd`) |
| `amount` | number | Transaction amount |

### Free text behavior

A query without a field name searches in:

- `title`
- `payee`
- `category`
- `categoryGroup`
- `comment`

If the text contains a decimal number token formatted like an amount (for example `12.00`), it can also match transactions with the exact same `amount`.

### Examples

```text
groceries
```

```text
title:"Monthly rent" AND amount<0
```

```text
date>=2026-01-01 AND date<=2026-01-31
```

```text
NOT (state=Reconciliated OR categoryGroup:"Internal transfer")
```

```text
accountId=3 AND payee:"Coffee shop"
```
