﻿using System;
using System.Globalization;
using System.Reflection;

namespace Meziantou.Moneiz
{
    [AttributeUsage(AttributeTargets.Assembly)]
    internal sealed class BuildDateAttribute : Attribute
    {
        public BuildDateAttribute(string value)
        {
            DateTime = DateTime.ParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        public DateTime DateTime { get; }

        public static DateTime? Get()
        {
            return typeof(BuildDateAttribute).Assembly.GetCustomAttribute<BuildDateAttribute>()?.DateTime;
        }
    }

    internal static class MoneizAppContext
    {
        public static string Version => typeof(MoneizAppContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        public static DateTime? BuildDate => BuildDateAttribute.Get();

        public static string Hash
        {
            get
            {
                var version = Version;
                if (version != null)
                {
                    var splits = version.Split('+');
                    if (splits.Length > 1)
                        return splits[1];
                }

                return null;
            }
        }
    }
}
