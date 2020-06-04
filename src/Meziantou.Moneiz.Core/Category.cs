namespace Meziantou.Moneiz.Core
{
    public sealed class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public override string ToString()
        {
            return Name ?? "";
        }
    }
}
