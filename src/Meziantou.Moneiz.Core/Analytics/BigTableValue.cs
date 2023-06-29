using System.Runtime.InteropServices;

namespace Meziantou.Moneiz.Core.Analytics
{
    [StructLayout(LayoutKind.Auto)]
    public struct BigTableValue
    {
        public decimal Incomes { get; set; }
        public decimal Expenses { get; set; }

        public readonly decimal Total => Incomes + Expenses;

        public void Add(decimal amount)
        {
            if (amount > 0m)
            {
                Incomes += amount;
            }
            else
            {
                Expenses += amount;
            }
        }

        public static BigTableValue operator +(BigTableValue a, BigTableValue b) => new()
        {
            Incomes = a.Incomes + b.Incomes,
            Expenses = a.Expenses + b.Expenses,
        };
    }
}
