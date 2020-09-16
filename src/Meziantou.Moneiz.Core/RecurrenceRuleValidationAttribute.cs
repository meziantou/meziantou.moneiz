using System;
using System.ComponentModel.DataAnnotations;
using Meziantou.Framework.Scheduling;

namespace Meziantou.Moneiz.Core
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public sealed class RecurrenceRuleValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            return value as string != null && RecurrenceRule.TryParse(value as string, out _);
        }
    }
}
