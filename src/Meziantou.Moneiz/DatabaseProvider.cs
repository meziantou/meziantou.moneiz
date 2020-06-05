using Meziantou.Moneiz.Core;
using Microsoft.JSInterop;
using System;
using System.IO;
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

        public event EventHandler DatabaseChanged;

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

            _database.DatabaseChanged += Database_DatabaseChanged;
            return _database;
        }

        public Task Save()
        {
            if (_database == null)
                throw new InvalidOperationException("Database is not loaded");

            return Save(_database, new SaveOptions { IndicateDbChanged = true });
        }

        private async Task Save(Database database, SaveOptions options)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(database);
            var content = Convert.ToBase64String(bytes);
            await _jsRuntime.InvokeVoidAsync("MoneizSaveDatabase", content, options);

            DatabaseChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task Export()
        {
            await _jsRuntime.InvokeVoidAsync("MoneizExportDatabase");
            RaiseDatabaseChanged();
        }

        public async Task Import(Stream stream)
        {
            var reader = new StreamReader(stream);
            var base64 = await reader.ReadToEndAsync();
            var content = Convert.FromBase64String(base64);
            var db = JsonSerializer.Deserialize<Database>(content);
            await Save(db, new SaveOptions { IndicateDbChanged = false });

            _database = null;
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Database_DatabaseChanged(object sender, EventArgs e)
        {
            RaiseDatabaseChanged();
        }

        private void RaiseDatabaseChanged()
        {
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task<bool> IsExported()
        {
            return await _jsRuntime.InvokeAsync<bool>("MoneizDatabaseIsExported");
        }
    }
}
