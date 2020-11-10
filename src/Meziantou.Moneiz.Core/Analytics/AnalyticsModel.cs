using System;
using System.Collections.Generic;
using System.Linq;

namespace Meziantou.Moneiz.Core.Analytics
{
    public sealed class AnalyticsModel
    {
        public IReadOnlyList<Account> Accounts { get; set; } = Array.Empty<Account>();
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }

        public BalanceHistory? BalanceHistory { get; set; }
        public BigTable? BigTable { get; set; }

        public static AnalyticsModel Build(Database database, IReadOnlyList<Account> accounts, DateTime fromDate, DateTime toDate)
        {
            return new AnalyticsModel
            {
                Accounts = accounts,
                PeriodFrom = fromDate,
                PeriodTo = toDate,
                BalanceHistory = BuildBalanceHistory(database, accounts, fromDate, toDate),
                BigTable = BuildBigTable(database, accounts, fromDate, toDate),
            };
        }

        private static BalanceHistory BuildBalanceHistory(Database database, IReadOnlyList<Account> accounts, DateTime fromDate, DateTime toDate)
        {
            var result = new BalanceHistory();

            foreach (var account in accounts)
            {
                var entry = new BalanceHistoryEntry
                {
                    Account = account,
                    Currency = account.CurrencyIsoCode,
                    StartBalance = database.GetBalance(account, fromDate),
                    EndBalance = database.GetBalance(account, toDate),
                };

                result.BalancesByAccount.Add(entry);
            }

            return result;
        }

        private static BigTable BuildBigTable(Database database, IEnumerable<Account> accounts, DateTime fromDate, DateTime toDate)
        {
            var transactionGroups = database.Transactions
                .Where(t => accounts.Contains(t.Account) && t.ValueDate >= fromDate && t.ValueDate <= toDate)
                .GroupBy(c => c.Category);

            var bigTable = new BigTable
            {
                Dates = new DateTime[(toDate.Year - fromDate.Year) * 12 + (toDate.Month - fromDate.Month) + 1],
            };
            for (var i = 0; i < bigTable.Dates.Length; i++)
            {
                bigTable.Dates[i] = fromDate.AddMonths(i);
            }

            foreach (var group in transactionGroups)
            {
                string? categoryName = null;
                string? categoryGroupName = null;
                if (group.Key != null)
                {
                    var category = group.Key;
                    categoryName = category.Name;
                    categoryGroupName = category.GroupName;
                }

                var bigTableGroup = bigTable.CategoryGroups.Find(g => string.Equals(g.Name, categoryGroupName, StringComparison.Ordinal));
                if (bigTableGroup == null)
                {
                    bigTableGroup = new BigTableCategoryGroup(bigTable)
                    {
                        Name = categoryGroupName,
                    };
                    bigTable.CategoryGroups.Add(bigTableGroup);
                }

                var bigTableCategory = new BigTableCategory(bigTableGroup)
                {
                    Name = categoryName,
                };
                bigTableGroup.Categories.Add(bigTableCategory);

                foreach (var transaction in group)
                {
                    var index = (transaction.ValueDate.Year - fromDate.Year) * 12 + (transaction.ValueDate.Month - fromDate.Month);
                    bigTableCategory.Totals[index].Add(transaction.Amount);
                }
            }

            bigTable.ComputeTotals();
            bigTable.CategoryGroups.Sort(NameComparer.Instance);
            foreach (var categoryGroup in bigTable.CategoryGroups)
            {
                categoryGroup.Categories.Sort(NameComparer.Instance);
            }

            return bigTable;
        }

        private sealed class NameComparer : IComparer<BigTableCategoryGroup>, IComparer<BigTableCategory>, IComparer<string>
        {
            public static NameComparer Instance { get; } = new NameComparer();

            public int Compare(BigTableCategoryGroup? x, BigTableCategoryGroup? y)
            {
                return Compare(x?.Name, y?.Name);
            }

            public int Compare(BigTableCategory? x, BigTableCategory? y)
            {
                return Compare(x?.Name, y?.Name);
            }

            public int Compare(string? x, string? y)
            {
                if (string.Equals(x, y, StringComparison.Ordinal))
                    return 0;

                if (x == null)
                    return 1;

                if (y == null)
                    return -1;

                return x.CompareTo(y);
            }
        }
    }

    public sealed class BalanceHistory
    {
        public IList<BalanceHistoryEntry> BalancesByAccount { get; } = new List<BalanceHistoryEntry>();
    }

    public sealed class BalanceHistoryEntry
    {
        public Account? Account { get; set; }
        public string? Currency { get; set; }

        public decimal StartBalance { get; set; }
        public decimal EndBalance { get; set; }

        public decimal Difference => EndBalance - StartBalance;
        public decimal DifferencePercentage => StartBalance != 0 ? (Difference / StartBalance * 100m) : 0m;
    }
}
