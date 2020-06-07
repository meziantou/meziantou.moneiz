using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    public sealed class Category
    {
        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        [JsonPropertyName("c")]
        public string? GroupName { get; set; }

        public override string ToString()
        {
            if (GroupName == null)
                return Name ?? "";

            return GroupName + "::" + Name;
        }
    }
}
