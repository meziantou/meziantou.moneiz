using System;
using System.Linq;

namespace Meziantou.Moneiz.Core.Analytics;

public sealed class BigTableCategory
{
    public BigTableCategory(BigTableCategoryGroup categoryGroup, int categoryId)
    {
        CategoryGroup = categoryGroup ?? throw new ArgumentNullException(nameof(categoryGroup));
        CategoryId = categoryId;
        Totals = new BigTableValue[categoryGroup.BigTable.Dates.Length];
    }

    public BigTableCategoryGroup CategoryGroup { get; }
    public int CategoryId { get; }
    public string? Name { get; set; }
    public BigTableValue[] Totals { get; }
    public BigTableValue Total { get; private set; }

    public string DisplayName => Name ?? "Unclassified";

    public void ComputeTotals()
    {
        Total = Totals.Aggregate(new BigTableValue(), (acc, value) => acc + value);
    }
}
