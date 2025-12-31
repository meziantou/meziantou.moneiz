using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Meziantou.Moneiz.Services;

public sealed class ChartService(IJSRuntime jsRuntime)
{
    private readonly IJSInProcessRuntime _jsRuntime = (IJSInProcessRuntime)jsRuntime;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public bool CreateLineChart(ElementReference canvasElement, string[] labels, IEnumerable<ChartDataset> datasets, ChartOptions? options = null)
    {
        var labelsJson = JsonSerializer.Serialize(labels, JsonOptions);
        var datasetsJson = JsonSerializer.Serialize(datasets, JsonOptions);
        var optionsJson = JsonSerializer.Serialize(options ?? new ChartOptions(), JsonOptions);
        return _jsRuntime.Invoke<bool>("MoneizCharts.createLineChart", canvasElement, labelsJson, datasetsJson, optionsJson);
    }

    public void DestroyChart(ElementReference canvasElement)
    {
        _jsRuntime.InvokeVoid("MoneizCharts.destroyChart", canvasElement);
    }
}

public sealed class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public double[] Data { get; set; } = Array.Empty<double>();
}

public sealed class ChartOptions
{
    public int LineWidth { get; set; } = 2;
}
