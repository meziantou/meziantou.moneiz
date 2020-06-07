using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    public sealed class Currency
    {
        [JsonPropertyName("a")]
        public string? IsoName { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        public override string ToString() => IsoName + " - " + Name;
    }
}
