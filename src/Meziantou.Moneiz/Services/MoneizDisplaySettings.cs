#nullable enable

using System;

namespace Meziantou.Moneiz.Services
{
    public class MoneizDisplaySettings
    {
        public int PageSize { get; set; } = 150;
        public string? DateFormat { get; set; }
        public bool FullWidth { get; set; }

        public string FormatDate(DateOnly date) => DateFormat == null ? date.ToShortDateString() : date.ToString(DateFormat);
        public string? FormatDate(DateOnly? date) => DateFormat == null ? date?.ToShortDateString() : date?.ToString(DateFormat);
        public string FormatDate(DateTime date) => DateFormat == null ? date.ToShortDateString() : date.ToString(DateFormat);
        public string? FormatDate(DateTime? date) => DateFormat == null ? date?.ToShortDateString() : date?.ToString(DateFormat);
    }
}
