namespace Meziantou.Moneiz.Extensions;

internal static class AmountUtilities
{
    public static string FormatAmount(decimal value)
    {
        return value.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
    }
}
