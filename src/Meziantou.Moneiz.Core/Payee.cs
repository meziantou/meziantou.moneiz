using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    public sealed class Payee
    {
        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        [JsonPropertyName("c")]
        public Category? DefaultCategory { get; set; }

        public override string ToString()
        {
            return Name ?? "";
        }
    }
}
