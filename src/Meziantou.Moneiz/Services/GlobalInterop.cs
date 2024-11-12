using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Meziantou.Moneiz.Services;

public static partial class GlobalInterop
{
    [JSImport("globalThis.confirm")]
    public static partial bool Confirm(string message);

    [JSImport("globalThis.confirm")]
    public static partial void Alert(string message);

    [JSImport("globalThis.MoneizDownloadFile")]
    public static partial Task ExportToFile(string filename, byte[] content);

    [JSImport("globalThis.MoneizSetValue")]
    public static partial Task SetValue(string name, string value);
    public static Task SetValue(string name, bool value) => SetValue(name, value ? "true" : "false");
    public static Task SetValue<T>(string name, T value, JsonTypeInfo<T> typeInfo) => SetValue(name, JsonSerializer.Serialize(value, typeInfo));
    public static Task SetValue(string name, byte[] value) => SetValue(name, value is null ? null : Convert.ToBase64String(value));

    [JSImport("globalThis.MoneizGetValue")]
    public static partial Task<string> GetValue(string name);

    public static async Task<bool> GetValue(string name, bool defaultValue)
    {
        var value = await GetValue(name);
        if (value is not null)
            return value is "true";

        return defaultValue;
    }

    public static async Task<T> GetValue<T>(string name, JsonTypeInfo<T> typeInfo)
    {
        var value = await GetValue(name);
        if (value is not null)
            return JsonSerializer.Deserialize(value, typeInfo);

        return default;
    }

    public static async Task<byte[]> GetByteArrayValue(string name)
    {
        var value = await GetValue(name);
        if (value is not null)
            return Convert.FromBase64String(value);

        return null;
    }

    public static void OpenInTab(string url) => Open(url, "_blank");

    [JSImport("globalThis.open")]
    private static partial void Open(string url, string option);
}
