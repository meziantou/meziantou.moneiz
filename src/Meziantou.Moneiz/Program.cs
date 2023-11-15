using System;
using System.Net.Http;
using Meziantou.Moneiz;
using Meziantou.Moneiz.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;


var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("app");

builder.Services.AddTransient(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<DatabaseProvider>();
builder.Services.AddSingleton<ConfirmService>();
builder.Services.AddSingleton<SettingsProvider>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
