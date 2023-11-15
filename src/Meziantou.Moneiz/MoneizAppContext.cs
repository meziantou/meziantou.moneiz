using System;
using System.Reflection;

namespace Meziantou.Moneiz;

internal static class MoneizAppContext
{
    public static string Version => typeof(MoneizAppContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
    public static DateTime? BuildDate => BuildDateAttribute.Get();

    public static string Hash
    {
        get
        {
            var version = Version;
            if (version is not null)
            {
                var splits = version.Split('+');
                if (splits.Length > 1)
                    return splits[1];
            }

            return null;
        }
    }
}
