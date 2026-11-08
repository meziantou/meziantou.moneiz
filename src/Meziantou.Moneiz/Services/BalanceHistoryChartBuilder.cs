using Meziantou.Framework.Charts;
using Meziantou.Moneiz.Core.Analytics;
using Meziantou.Moneiz.Extensions;

namespace Meziantou.Moneiz.Services;

internal static class BalanceHistoryChartBuilder
{
    private const string IdPrefix = "balance-chart-";

    public static IReadOnlyList<BalanceHistoryChartData> Build(BalanceHistory? model, MoneizDisplaySettings displaySettings)
    {
        if (model is null || model.BalancesByAccount.Count == 0)
            return [];

        var length = model.BalancesByAccount[0].Balances!.Length;
        var currencySeries = new List<ChartSeries>();
        var accountCharts = new List<(string Name, List<ChartSeries> Series)>();
        foreach (var group in model.BalancesByAccount.GroupBy(entry => entry.Currency))
        {
            var currencyLabel = !string.IsNullOrEmpty(group.Key) ? group.Key : "No currency";
            var currencyBalances = new decimal[length];
            var accountSeries = new List<ChartSeries>();
            foreach (var entry in group)
            {
                var balances = entry.Balances!;
                accountSeries.Add(new ChartSeries(entry.Account!.ToString(), group.Key, balances));
                for (var i = 0; i < balances.Length; i++)
                {
                    currencyBalances[i] += balances[i];
                }
            }

            currencySeries.Add(new ChartSeries(currencyLabel, group.Key, currencyBalances));
            accountCharts.Add(($"Balance - {currencyLabel}", accountSeries));
        }

        var result = new List<BalanceHistoryChartData>(accountCharts.Count + 1)
        {
            CreateChart("Balance by currencies", currencySeries, model.StartDate, displaySettings, IdPrefix + "0"),
        };

        for (var i = 0; i < accountCharts.Count; i++)
        {
            var (name, series) = accountCharts[i];
            result.Add(CreateChart(name, series, model.StartDate, displaySettings, IdPrefix + (i + 1).ToString(CultureInfo.InvariantCulture)));
        }

        return result;
    }

    private static BalanceHistoryChartData CreateChart(string name, List<ChartSeries> series, DateOnly startDate, MoneizDisplaySettings displaySettings, string idPrefix)
    {
        var points = new List<BalancePoint>();
        foreach (var (seriesName, currency, balances) in series)
        {
            // Only keep the days where the balance changes, plus the first and the last days, to keep the SVG small
            for (var i = 0; i < balances.Length; i++)
            {
                if (i is 0 || i == balances.Length - 1 || balances[i] != balances[i - 1])
                {
                    decimal? change = i is 0 ? null : balances[i] - balances[i - 1];
                    points.Add(new BalancePoint(seriesName, currency, startDate.AddDays(i), balances[i], change));
                }
            }
        }

        var plot = new Plot
        {
            Width = 1100,
            Height = 350,
            Description = name,
            Theme = ChartTheme.Light with { Background = null },
            XScale = new TimeScale(),
            ColorScale = new OrdinalColorScale { Domain = [.. series.Select(s => s.Name)] },
        };
        plot.Legend.Visible = true;
        plot.Add(new LineMark<BalancePoint>(points, x: point => point.Timestamp, y: point => (double)point.Value)
        {
            Stroke = point => point.Series,
        });
        plot.Add(new DotMark<BalancePoint>(points, x: point => point.Timestamp, y: point => (double)point.Value)
        {
            Fill = point => point.Series,
            Radius = 3,

            // The title is the tooltip when the script of the page does not run (see wwwroot/js/charts.js)
            Title = point => $"{point.Series}\n{displaySettings.FormatDate(point.Date)}: {AmountUtilities.FormatAmount(point.Value)}",
            DataAttributes = point =>
            [
                new("series", point.Series),
                new("date", displaySettings.FormatDate(point.Date)),
                new("amount", point.Value),
                new("change", point.Change),
                new("currency", point.Currency),
            ],
            CssClass = "chart-points",
        });

        var svg = plot.ToSvg(new SvgRenderOptions { IdPrefix = idPrefix, IncludeNamespace = false });
        return new BalanceHistoryChartData(name, svg);
    }

    private sealed record ChartSeries(string Name, string? Currency, decimal[] Balances);

    private sealed record BalancePoint(string Series, string? Currency, DateOnly Date, decimal Value, decimal? Change)
    {
        public DateTime Timestamp => Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    }
}

internal sealed record BalanceHistoryChartData(string Name, string Svg);
