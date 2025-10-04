namespace Meziantou.Moneiz.Core.Analytics;

public sealed class BigTableCategory(BigTableCategoryGroup categoryGroup, int categoryId)
{
    public BigTableCategoryGroup CategoryGroup { get; } = categoryGroup ?? throw new ArgumentNullException(nameof(categoryGroup));
    public int CategoryId { get; } = categoryId;
    public string? Name { get; set; }
    public BigTableValue[] Totals { get; } = new BigTableValue[categoryGroup.BigTable.Dates.Length];
    public BigTableValue Total { get; private set; }

    public string DisplayName => Name ?? "Unclassified";

    public void ComputeTotals()
        => Total = Totals.Aggregate(new BigTableValue(), (acc, value) => acc + value);
}
