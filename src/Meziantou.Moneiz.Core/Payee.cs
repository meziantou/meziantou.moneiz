using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core;

public sealed class Payee
{
    private int? _defaultCategoryId;

    [JsonPropertyName("a")]
    public int Id { get; set; }

    [JsonPropertyName("b")]
    public string? Name { get; set; }

    [JsonIgnore]
    public Category? DefaultCategory
    {
        get;
        set
        {
            field = value;
            _defaultCategoryId = null;
        }
    }

    [JsonPropertyName("c")]
    public int? DefaultCategoryId
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
