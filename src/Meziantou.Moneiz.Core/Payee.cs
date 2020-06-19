using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core
{
    public sealed class Payee
    {
        private int? _defaultCategoryId;
        private Category? _defaultCategory;

        [JsonPropertyName("a")]
        public int Id { get; set; }

        [JsonPropertyName("b")]
        public string? Name { get; set; }

        [JsonPropertyName("c")]
        public Category? DefaultCategory
        {
            get => _defaultCategory;
            set
            {
                _defaultCategory = value;
                _defaultCategoryId = null;
            }
        }

        [JsonPropertyName("d")]
        private int? DefaultCategoryId
        {
            get => DefaultCategory?.Id ?? _defaultCategoryId;
            set => _defaultCategoryId = value;
        }

        internal void ResolveReferences(Database database)
        {
            if (_defaultCategoryId.HasValue)
            {
                DefaultCategory = database.GetCategoryById(_defaultCategoryId);
            }
        }

        public override string ToString()
        {
            return Name ?? "";
        }
    }
}
