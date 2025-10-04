using Meziantou.Framework;

namespace Meziantou.Moneiz.Core;

public static class StringExtensions
{
    public static string? TrimAndNullify(this string? value)
    {
        if (value is null)
            return null;

        var result = value.AsSpan().Trim();
        if (result.IsEmpty)
            return null;

        return result.ToString();
    }
}
