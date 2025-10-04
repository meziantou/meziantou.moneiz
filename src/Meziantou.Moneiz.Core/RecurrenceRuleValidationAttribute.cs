using System.ComponentModel.DataAnnotations;
using Meziantou.Framework.Scheduling;

namespace Meziantou.Moneiz.Core;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class RecurrenceRuleValidationAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is null)
            return true;

        return value is string valueStr && RecurrenceRule.TryParse(valueStr, out _);
    }
}
