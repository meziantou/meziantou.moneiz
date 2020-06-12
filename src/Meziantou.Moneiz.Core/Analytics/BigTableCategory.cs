using System;
using System.Linq;

namespace Meziantou.Moneiz.Core.Analytics
{
    public sealed class BigTableCategory
    {
        public BigTableCategory(BigTableCategoryGroup categoryGroup)
        {
            CategoryGroup = categoryGroup ?? throw new ArgumentNullException(nameof(categoryGroup));
            Totals = new BigTableValue[categoryGroup.BigTable.Dates.Length];
        }

        public BigTableCategoryGroup CategoryGroup { get; }
        public string? Name { get; set; }
        public BigTableValue[] Totals { get; }
        public BigTableValue Total { get; private set; }

        public string DisplayName => Name ?? "Unclassified";

        public void ComputeTotals()
        {
            Total = Totals.Aggregate(new BigTableValue(), (acc, value) => acc + value);
        }
    }
}
