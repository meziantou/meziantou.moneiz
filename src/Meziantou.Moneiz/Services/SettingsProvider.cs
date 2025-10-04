using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Meziantou.Framework;

namespace Meziantou.Moneiz.Services;

public static partial class SettingsProvider
{
    private static Task<T?> GetValue<T>(string key, JsonTypeInfo<T> typeInfo) => GlobalInterop.GetValue("settings:" + key, typeInfo);

    private static Task SetValue<T>(string key, T value, JsonTypeInfo<T> typeInfo) => GlobalInterop.SetValue("settings:" + key, value, typeInfo);

    public static Task<bool?> GetShowReconciliatedTransactions(int accountId) => GetValue("account:" + accountId.ToStringInvariant() + ":ShowReconciled", JsonSettingsContext.Default.NullableBoolean);

    public static Task SetShowReconciliatedTransactions(int accountId, bool value) => SetValue("account:" + accountId.ToStringInvariant() + ":ShowReconciled", value, JsonSettingsContext.Default.NullableBoolean);

    public static async Task<MoneizDisplaySettings> GetDisplaySettings()
    {
        var result = await GetValue("displaySettings", JsonSettingsContext.Default.MoneizDisplaySettings);
        result ??= new MoneizDisplaySettings();
        result.PageSize = Math.Clamp(result.PageSize, 10, int.MaxValue);
        if (string.IsNullOrWhiteSpace(result.DateFormat))
        {
            result.DateFormat = null;
        }

        return result;
    }

    public static Task SetDisplaySettings(MoneizDisplaySettings value) => SetValue("displaySettings", value, JsonSettingsContext.Default.MoneizDisplaySettings);

    [JsonSerializable(typeof(MoneizDisplaySettings))]
    [JsonSerializable(typeof(bool?))]
    private sealed partial class JsonSettingsContext : JsonSerializerContext;
}
