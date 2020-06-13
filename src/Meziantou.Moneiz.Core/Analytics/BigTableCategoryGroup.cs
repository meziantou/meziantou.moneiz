using System;
using System.Collections.Generic;
using System.Linq;

namespace Meziantou.Moneiz.Core.Analytics
{
    public sealed class BigTableCategoryGroup
    {
        private BigTableValue _total;

        public BigTableCategoryGroup(BigTable bigTable)
        {
            BigTable = bigTable ?? throw new ArgumentNullException(nameof(bigTable));
        }

        public BigTable BigTable { get; }
        public string? Name { get; set; }
        public List<BigTableCategory> Categories { get; } = new List<BigTableCategory>();

        public string DisplayName => Name ?? "Unclassified";
        public bool IsUnclassified => Name == null;
        public BigTableValue[] Totals { get; private set; } = Array.Empty<BigTableValue>();
        public BigTableValue Total => _total;

        public void ComputeTotals()
        {
            foreach (var item in Categories)
            {
                item.ComputeTotals();
            }

            var amounts = new BigTableValue[BigTable.Dates.Length];
            for (var i = 0; i < amounts.Length; i++)
            {
                var sum = new BigTableValue();
                foreach (var item in Categories)
                {
                    sum += item.Totals[i];
                }

                amounts[i] = sum;
            }

            Totals = amounts;
            _total = amounts.Aggregate(new BigTableValue(), (acc, value) => acc + value);
        }
    }
}
