#nullable enable

using System;
using System.Globalization;

namespace Meziantou.Moneiz.Services;

public class MoneizDisplaySettings
{
    public int PageSize { get; set; } = 150;
    public string? DateFormat { get; set; }
    public bool FullWidth { get; set; }

    public string FormatDate(DateOnly date) => DateFormat is null ? date.ToShortDateString() : date.ToString(DateFormat, CultureInfo.CurrentCulture);
    public string? FormatDate(DateOnly? date) => DateFormat is null ? date?.ToShortDateString() : date?.ToString(DateFormat, CultureInfo.CurrentCulture);
    public string FormatDate(DateTime date) => DateFormat is null ? date.ToShortDateString() : date.ToString(DateFormat, CultureInfo.CurrentCulture);
    public string? FormatDate(DateTime? date) => DateFormat is null ? date?.ToShortDateString() : date?.ToString(DateFormat, CultureInfo.CurrentCulture);
}
