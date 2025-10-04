using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core.V1;

internal sealed class Transaction
{
    [JsonPropertyName("a")]
    public int Id { get; set; }

    [JsonPropertyName("b")]
    public decimal Amount { get; set; }

    [JsonPropertyName("c")]
    public string? Comment { get; set; }

    [JsonPropertyName("d")]
    public DateOnly ValueDate { get; set; }

    [JsonPropertyName("e")]
    public DateOnly? CheckedDate { get; set; }

    [JsonPropertyName("f")]
    public DateTime? ReconciliationDate { get; set; }

    [JsonPropertyName("g")]
    public Account? Account { get; set; }

    [JsonPropertyName("h")]
    public Payee? Payee { get; set; }

    [JsonPropertyName("i")]
    public Category? Category { get; set; }

    [JsonPropertyName("j")]
    public Transaction? LinkedTransaction { get; set; }
}
