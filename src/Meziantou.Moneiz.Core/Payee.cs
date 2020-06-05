namespace Meziantou.Moneiz.Core
{
    public sealed class Payee
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Category? DefaultCategory { get; set; }

        public override string ToString()
        {
            return Name ?? "";
        }
    }
}
