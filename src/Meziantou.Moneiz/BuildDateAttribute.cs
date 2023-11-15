using System;
using System.Globalization;
using System.Reflection;

namespace Meziantou.Moneiz;

[AttributeUsage(AttributeTargets.Assembly)]
[method: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019:Define accessors for attribute arguments")]
internal sealed class BuildDateAttribute(string value) : Attribute
{
    public DateTime DateTime { get; } = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);

    public static DateTime? Get()
    {
        return typeof(BuildDateAttribute).Assembly.GetCustomAttribute<BuildDateAttribute>()?.DateTime;
    }
}
