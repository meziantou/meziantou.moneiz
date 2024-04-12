namespace Meziantou.Moneiz.Core;

public sealed class Currency(string isoName, string displayName, decimal exchangeRate)
{
    public string? IsoName { get; set; } = isoName;
    public string? DisplayName { get; set; } = displayName;
    public decimal ExchangeRate { get; } = exchangeRate;

    public override string ToString() => IsoName + " - " + DisplayName;
}
