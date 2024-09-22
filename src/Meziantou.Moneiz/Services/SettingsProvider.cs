using System;
using System.Threading.Tasks;
using Meziantou.Framework;
using Meziantou.Moneiz.Extensions;
using Microsoft.JSInterop;

namespace Meziantou.Moneiz.Services;

public static class SettingsProvider
{
    private static Task<T> GetValue<T>(string key) => GlobalInterop.GetValue<T>("settings:" + key);

    private static Task SetValue<T>(string key, T value) => GlobalInterop.SetValue("settings:" + key, value);

    public static Task<bool?> GetShowReconciliatedTransactions(int accountId) => GetValue<bool?>("account:" + accountId.ToStringInvariant() + ":ShowReconciled");

    public static Task SetShowReconciliatedTransactions(int accountId, bool value) => SetValue("account:" + accountId.ToStringInvariant() + ":ShowReconciled", value);

    public static async Task<MoneizDisplaySettings> GetDisplaySettings()
    {
        var result = await GetValue<MoneizDisplaySettings>("displaySettings");
        result ??= new MoneizDisplaySettings();
        result.PageSize = Math.Clamp(result.PageSize, 10, int.MaxValue);
        if (string.IsNullOrWhiteSpace(result.DateFormat))
        {
            result.DateFormat = null;
        }

        return result;
    }

    public static Task SetDisplaySettings(MoneizDisplaySettings value) => SetValue("displaySettings", value);
}
