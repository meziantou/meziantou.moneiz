using System.Collections.Generic;

namespace Meziantou.Moneiz.Core
{
    public sealed class CategoryGroup
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public IList<Category> Categories { get; set; } = new List<Category>();

        public override string ToString()
        {
            return Name ?? "";
        }
    }
}
