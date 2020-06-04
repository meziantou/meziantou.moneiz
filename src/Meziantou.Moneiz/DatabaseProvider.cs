using Meziantou.Moneiz.Core;
using Microsoft.JSInterop;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Moneiz
{
    public class DatabaseProvider : IDatabaseProvider, IDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private Database _database;


        public DatabaseProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        public async Task<Database> GetDatabase()
        {
            if (_database == null)
            {
                await _semaphore.WaitAsync();
                try
                {
                    if (_database == null)
                    {
                        var result = await _jsRuntime.InvokeAsync<string>("MoneizLoadDatabase");
                        if (!string.IsNullOrEmpty(result))
                        {
                            var content = Convert.FromBase64String(result);
                            _database = JsonSerializer.Deserialize<Database>(content);
                        }

                        if (_database == null)
                        {
                            _database = new Database();
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            if (_database.Currencies.Count == 0)
            {
                _database.Currencies.Add(new Currency { Name = "Euro", IsoName = "EUR" });
                _database.Currencies.Add(new Currency { Name = "Canadian Dollar", IsoName = "CAD" });
                _database.Currencies.Add(new Currency { Name = "Great Britain Pound", IsoName = "GBP" });
                _database.Currencies.Add(new Currency { Name = "United States dollar", IsoName = "USD" });
            }

            return _database;
        }

        public async Task Save()
        {
            if (_database == null)
                throw new InvalidOperationException("Database is not loaded");

            var bytes = JsonSerializer.SerializeToUtf8Bytes(_database);
            var content = Convert.ToBase64String(bytes);
            await _jsRuntime.InvokeVoidAsync("MoneizSaveDatabase", content);
        }
    }
}
