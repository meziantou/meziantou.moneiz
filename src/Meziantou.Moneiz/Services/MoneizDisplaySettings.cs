#nullable enable

using Meziantou.Moneiz.Core;

namespace Meziantou.Moneiz.Services;

public enum CategoryDisplayMode
{
    Name,
    FullName,
}

public class MoneizDisplaySettings
{
    public int PageSize { get; set; } = 150;
    public string? DateFormat { get; set; }
    public bool FullWidth { get; set; }
    public bool SidebarCollapsed { get; set; }
    public int? SidebarMaxWidth { get; set; } = 400;
    public CategoryDisplayMode CategoryDisplayMode { get; set; } = CategoryDisplayMode.Name;
    public bool ShowLabels { get; set; } = true;

    public string FormatDate(DateOnly date) => DateFormat is null ? date.ToShortDateString() : date.ToString(DateFormat, CultureInfo.InvariantCulture);
    public string? FormatDate(DateOnly? date) => DateFormat is null ? date?.ToShortDateString() : date?.ToString(DateFormat, CultureInfo.InvariantCulture);
    public string FormatDate(DateTime date) => DateFormat is null ? date.ToShortDateString() : date.ToString(DateFormat, CultureInfo.InvariantCulture);
    public string? FormatDate(DateTime? date) => DateFormat is null ? date?.ToShortDateString() : date?.ToString(DateFormat, CultureInfo.InvariantCulture);

    public string? FormatCategory(Category? category)
    {
        if (category is null)
        {
            return null;
        }

        return CategoryDisplayMode switch
        {
            CategoryDisplayMode.FullName => category.ToString(),
            _ => category.Name ?? "",
        };
    }
}
