namespace Meziantou.Moneiz.Core.Analytics;

public sealed class AnalyticsOptions
{
    public event EventHandler? OptionChanged;

    public DateOnly FromDate { get; set; } = Database.GetToday().AddDays(-30);
    public DateOnly ToDate { get; set; } = Database.GetToday();
    public BigTableDateGrouping BigTableDateGrouping { get; set; } = BigTableDateGrouping.Month;
    public ISet<Account> SelectedAccounts { get; } = new HashSet<Account>();

    public ISet<string> SelectedLabels { get; } = new HashSet<string>(StringComparer.Ordinal);

    public ISet<int> BigTableDisabledCategories { get; } = new HashSet<int>();
    public ISet<string> BigTableDisabledCategoryGroups { get; } = new HashSet<string>(StringComparer.Ordinal);
    public ISet<string> BigTableCollapsedCategoryGroups { get; } = new HashSet<string>(StringComparer.Ordinal);

    public bool IsLabelFilterActive => SelectedLabels.Count > 0;

    public bool IsLabelEnabled(string label) => SelectedLabels.Count == 0 || SelectedLabels.Contains(label);

    public bool TransactionMatchesLabelFilter(Transaction transaction)
    {
        if (SelectedLabels.Count == 0)
            return true;

        if (transaction.Labels is null || transaction.Labels.Length == 0)
            return false;

        return transaction.Labels.Any(l => SelectedLabels.Contains(l));
    }

    public bool IsGroupEnabled(string? name) => !BigTableDisabledCategoryGroups.Contains(name ?? "");
    public bool IsCategoryEnabled(int categoryId) => !BigTableDisabledCategories.Contains(categoryId);
    public bool IsGroupCollapsed(string? name) => BigTableCollapsedCategoryGroups.Contains(name ?? "");

    public void ToggleLabel(string label)
    {
        if (!SelectedLabels.Remove(label))
        {
            _ = SelectedLabels.Add(label);
        }

        OptionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SelectAllLabels()
    {
        SelectedLabels.Clear();
        OptionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleDisabledCategory(int categoryId)
    {
        if (!BigTableDisabledCategories.Remove(categoryId))
        {
            _ = BigTableDisabledCategories.Add(categoryId);
        }

        OptionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleDisabledCategoryGroup(string? name)
    {
        name ??= "";
        if (!BigTableDisabledCategoryGroups.Remove(name))
        {
            _ = BigTableDisabledCategoryGroups.Add(name);
        }

        OptionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ToggleCollapsedCategoryGroup(string? name)
    {
        name ??= "";
        if (!BigTableCollapsedCategoryGroups.Remove(name))
        {
            _ = BigTableCollapsedCategoryGroups.Add(name);
        }

        OptionChanged?.Invoke(this, EventArgs.Empty);
    }
    public void CollapseCategoryGroups(IEnumerable<string?> names)
    {
        BigTableCollapsedCategoryGroups.Clear();

        foreach (var name in names)
        {
            _ = BigTableCollapsedCategoryGroups.Add(name ?? "");
        }

        OptionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ExpandAllCategoryGroups()
    {
        BigTableCollapsedCategoryGroups.Clear();
        OptionChanged?.Invoke(this, EventArgs.Empty);
    }
}

public enum BigTableDateGrouping
{
    Month,
    Quarter,
    Year,
}
