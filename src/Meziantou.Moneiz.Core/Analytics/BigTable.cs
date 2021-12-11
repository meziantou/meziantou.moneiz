using System;
using System.Collections.Generic;
using System.Linq;

namespace Meziantou.Moneiz.Core.Analytics
{
    public sealed class BigTable
    {
        public DateOnly[] Dates { get; set; } = Array.Empty<DateOnly>();
        public List<BigTableCategoryGroup> CategoryGroups { get; } = new List<BigTableCategoryGroup>();

        public BigTableValue[] Totals { get; private set; } = Array.Empty<BigTableValue>();
        public BigTableValue Total { get; private set; }

        public void ComputeTotals()
        {
            foreach (var item in CategoryGroups)
            {
                item.ComputeTotals();
            }

            var totals = new BigTableValue[Dates.Length];
            for (var i = 0; i < totals.Length; i++)
            {
                var sum = new BigTableValue();
                foreach (var item in CategoryGroups)
                {
                    sum += item.Totals[i];
                }

                totals[i] = sum;
            }

            Totals = totals;
            Total = totals.Aggregate(new BigTableValue(), (acc, value) => acc + value);
        }
    }
}
