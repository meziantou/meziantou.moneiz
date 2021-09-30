using System;
using System.Threading.Tasks;
using Meziantou.Framework;
using Meziantou.Moneiz.Extensions;
using Microsoft.JSInterop;

namespace Meziantou.Moneiz.Services
{
    public sealed class SettingsProvider
    {
        private readonly IJSRuntime _jsRuntime;

        public SettingsProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        private ValueTask<T> GetValue<T>(string key)
        {
            return _jsRuntime.GetValue<T>("settings:" + key);
        }

        private ValueTask SetValue<T>(string key, T value)
        {
            return _jsRuntime.SetValue("settings:" + key, value);
        }

        public ValueTask<bool?> GetShowReconciliatedTransactions(int accountId)
        {
            return GetValue<bool?>("account:" + accountId.ToStringInvariant() + ":ShowReconciled");
        }

        public ValueTask SetShowReconciliatedTransactions(int accountId, bool value)
        {
            return SetValue("account:" + accountId.ToStringInvariant() + ":ShowReconciled", value);
        }

        public async ValueTask<MoneizDisplaySettings> GetDisplaySettings()
        {
            var result = await GetValue<MoneizDisplaySettings>("displaySettings");
            result ??= new MoneizDisplaySettings();
            result.PageSize = Math.Clamp(result.PageSize, 10, int.MaxValue);
            return result;
        }

        public ValueTask SetDisplaySettings(MoneizDisplaySettings value)
        {
            return SetValue("displaySettings", value);
        }
    }
}
