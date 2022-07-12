using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Meziantou.Framework.SimpleQueryLanguage;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Extensions;

namespace Meziantou.Moneiz.Pages.Transactions
{
    internal static class TransactionQueries
    {
        public static QueryBuilder<Transaction> TransactionQuery { get; } = CreateTransactionQuery();

        private static QueryBuilder<Transaction> CreateTransactionQuery()
        {
            var builder = new QueryBuilder<Transaction>();
            builder.AddHandler("title", (transaction, text) => transaction.FinalTitle?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
            builder.AddHandler("payee", (transaction, text) => transaction.Payee?.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
            builder.AddHandler("category", (transaction, text) => transaction.Category?.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
            builder.AddHandler("comment", (transaction, text) => transaction.Comment?.Contains(text, StringComparison.OrdinalIgnoreCase) == true);
            builder.AddHandler<TransactionState>("state", (transaction, value) => transaction.State == value);
            builder.AddRangeHandler<DateOnly>("date", (transaction, range) => range.IsInRange(transaction.ValueDate));
            builder.AddRangeHandler<decimal>("amount", (transaction, range) => range.IsInRange(transaction.Amount));

            builder.SetTextFilterHandler((transaction, text) =>
            {
                var amount = ExtractDecimalValues(text)?.LastOrDefault();
                if (transaction.FinalTitle?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                if (transaction.Payee?.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                if (transaction.Category?.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                if (transaction.Category?.GroupName?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                if (transaction.Comment?.Contains(text, StringComparison.OrdinalIgnoreCase) == true)
                    return true;

                if (amount.HasValue)
                {
                    if (transaction.Amount == amount)
                        return true;
                }

                return false;
            });

            return builder;
        }

        private static IEnumerable<decimal> ExtractDecimalValues(string text)
        {
            var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var part in parts)
            {
                // Ensure the value is formatted as it is displayed (12.00 instead of 12)
                // Use same format as in Amount.razor
                if (decimal.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && part == AmountUtilities.FormatAmount(result))
                    yield return result;
            }
        }
    }
}
