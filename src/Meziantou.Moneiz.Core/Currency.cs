namespace Meziantou.Moneiz.Core
{
    public sealed class Currency
    {
        public string? IsoName { get; set; }
        public string? Name { get; set; }

        public override string ToString() => IsoName + " - " + Name;
    }
}
