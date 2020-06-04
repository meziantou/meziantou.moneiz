using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Extensions;

namespace Meziantou.Moneiz
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddSingleton<IDatabaseProvider, DatabaseProvider>();
            builder.Services.AddSingleton<ConfirmService>();

            await builder.Build().RunAsync();
        }
    }
}
