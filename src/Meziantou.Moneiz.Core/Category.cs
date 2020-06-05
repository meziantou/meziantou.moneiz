namespace Meziantou.Moneiz.Core
{
    public sealed class Category
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? GroupName { get; set; }

        public override string ToString()
        {
            if (GroupName == null)
                return Name ?? "";

            return GroupName + "::" + Name;
        }
    }
}
