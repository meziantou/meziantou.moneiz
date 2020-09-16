using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Meziantou.Moneiz.Extensions
{
    internal static class JSRuntimeExtensions
    {
        public static ValueTask OpenInTab(this IJSRuntime jsRuntime, string url)
        {
            return jsRuntime.InvokeVoidAsync("MoneizOpenInTab", url);
        }
        
        public static ValueTask<string> GetValue(this IJSRuntime jsRuntime, string name)
        {
            return jsRuntime.InvokeAsync<string>("MoneizGetValue", name);
        }

        public static ValueTask SetValue(this IJSRuntime jsRuntime, string name, string value)
        {
            return jsRuntime.InvokeVoidAsync("MoneizSetValue", name, value);
        }

        public static ValueTask ExportToFile(this IJSUnmarshalledRuntime jsRuntime, string filename, byte[] content)
        {
            jsRuntime.InvokeUnmarshalled<string, byte[], bool>("MoneizDownloadFile", filename, content);
            return ValueTask.CompletedTask;
        }

        public static ValueTask SetValue<T>(this IJSRuntime jsRuntime, string name, T value)
        {
            return SetValue(jsRuntime, name, JsonSerializer.Serialize(value));
        }

        public static async ValueTask<T> GetValue<T>(this IJSRuntime jsRuntime, string name)
        {
            var value = await GetValue(jsRuntime, name);
            if (value != null)
                return JsonSerializer.Deserialize<T>(value);

            return default;
        }

        public static ValueTask SetValue(this IJSRuntime jsRuntime, string name, byte[] value)
        {
            return SetValue(jsRuntime, name, value == null ? null : Convert.ToBase64String(value));
        }

        public static async ValueTask<byte[]> GetByteArrayValue(this IJSRuntime jsRuntime, string name)
        {
            var value = await GetValue(jsRuntime, name);
            if (value != null)
                return Convert.FromBase64String(value);

            return null;
        }
    }
}
