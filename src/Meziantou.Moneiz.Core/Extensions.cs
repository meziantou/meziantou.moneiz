using System.Collections.Generic;
using System.Linq;

namespace Meziantou.Moneiz.Core
{
    public static class Extensions
    {
        public static IEnumerable<Account> Sort(this IEnumerable<Account> accounts)
        {
            return accounts
                .OrderBy(a => a.Closed)
                .ThenBy(a => a.SortOrder)
                .ThenBy(a => a.Name);
        }
    }
}
