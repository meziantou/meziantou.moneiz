using System;
using System.Net.Http;
using System.Threading.Tasks;
using Meziantou.Moneiz.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Meziantou.Moneiz
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton<DatabaseProvider>();
            builder.Services.AddSingleton<ConfirmService>();
            builder.Services.AddSingleton<SettingsProvider>();

            await builder.Build().RunAsync();
        }
    }
}
