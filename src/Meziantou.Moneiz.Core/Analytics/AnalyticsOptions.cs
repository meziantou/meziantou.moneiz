using System;
using System.Collections.Generic;

namespace Meziantou.Moneiz.Core.Analytics;

public sealed class AnalyticsOptions
{
    public event EventHandler? OptionChanged;

    public DateOnly FromDate { get; set; } = Database.GetToday().AddDays(-30);
    public DateOnly ToDate { get; set; } = Database.GetToday();
    public ISet<Account> SelectedAccounts { get; } = new HashSet<Account>();

    public ISet<int> BigTableDisabledCategories { get; } = new HashSet<int>();
    public ISet<string> BigTableDisabledCategoryGroups { get; } = new HashSet<string>(StringComparer.Ordinal);

    public bool IsGroupEnabled(string? name) => !BigTableDisabledCategoryGroups.Contains(name ?? "");
    public bool IsCategoryEnabled(int categoryId) => !BigTableDisabledCategories.Contains(categoryId);

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
}
