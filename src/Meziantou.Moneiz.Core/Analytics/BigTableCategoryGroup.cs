namespace Meziantou.Moneiz.Core.Analytics;

public sealed class BigTableCategoryGroup(BigTable bigTable)
{
    private BigTableValue _total;

    public BigTable BigTable { get; } = bigTable ?? throw new ArgumentNullException(nameof(bigTable));
    public string? Name { get; set; }
    public List<BigTableCategory> Categories { get; } = [];

    public string DisplayName => Name ?? "Unclassified";
    public bool IsUnclassified => Name is null;
    public BigTableValue[] Totals { get; private set; } = [];
    public BigTableValue Total => _total;

    public void ComputeTotals()
    {
        foreach (var item in Categories)
        {
            item.ComputeTotals();
        }

        var amounts = new BigTableValue[BigTable.Dates.Length];
        for (var i = 0; i < amounts.Length; i++)
        {
            var sum = new BigTableValue();
            foreach (var item in Categories)
            {
                sum += item.Totals[i];
            }

            amounts[i] = sum;
        }

        Totals = amounts;
        _total = amounts.Aggregate(new BigTableValue(), (acc, value) => acc + value);
    }
}
