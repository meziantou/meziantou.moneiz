namespace Meziantou.Moneiz.Core;

public sealed class Currency
{
    public Currency(string isoName, string displayName, decimal exchangeRate)
    {
        IsoName = isoName;
        DisplayName = displayName;
        ExchangeRate = exchangeRate;
    }

    public string? IsoName { get; set; }
    public string? DisplayName { get; set; }
    public decimal ExchangeRate { get; }

    public override string ToString() => IsoName + " - " + DisplayName;
}
