using Meziantou.Framework;
using Meziantou.Moneiz.Core;
using Meziantou.Moneiz.Extensions;
using Meziantou.Moneiz.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Meziantou.Moneiz
{
    public sealed class DatabaseProvider : IDisposable
    {
        private const string MoneizLocalStorageDbName = "moneiz.db";
        private const string MoneizLocalStorageConfigurationName = "moneiz.configuration";
        private const string MoneizLocalStorageChangedName = "moneiz.dbchanged";

        private const string MoneizDownloadFileName = "moneiz.db";

        private readonly IJSRuntime _jsRuntime;
        private readonly ConfirmService _confirmService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private Database _database;

        public event EventHandler DatabaseChanged;
        public event EventHandler DatabaseSaved;

        public DatabaseProvider(IJSRuntime jsRuntime, ConfirmService confirmService)
        {
            _jsRuntime = jsRuntime;
            _confirmService = confirmService;
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
                        var configuration = await LoadConfiguration();
                        if (configuration.GitHubAutoLoad && !string.IsNullOrWhiteSpace(configuration?.GitHubToken))
                        {
                            await ImportFromGitHub(implicitLoad: true);
                        }

                        if (_database == null)
                        {
                            var result = await _jsRuntime.GetByteArrayValue(MoneizLocalStorageDbName);
                            if (result != null)
                            {
                                _database = await Database.Load(result);
                            }

                            if (_database == null)
                            {
                                _database = new Database();
                            }
                        }

                        _database.DatabaseChanged += Database_DatabaseChanged;
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }

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
            var stopwatch = ValueStopwatch.StartNew();
            Console.WriteLine("Saving database to local storage");
            var bytes = database.Export();
            Console.WriteLine("Database blob generated in " + stopwatch.GetElapsedTime());

            stopwatch = ValueStopwatch.StartNew();
            await _jsRuntime.SetValue(MoneizLocalStorageDbName, bytes);
            Console.WriteLine("Database blob saved in " + stopwatch.GetElapsedTime());

            stopwatch = ValueStopwatch.StartNew();
            await _jsRuntime.SetValue(MoneizLocalStorageChangedName, options.IndicateDbChanged);
            Console.WriteLine("Database changed saved in " + stopwatch.GetElapsedTime());

            stopwatch = ValueStopwatch.StartNew();
            RaiseDatabaseSaved();
            Console.WriteLine("Database changed notification handled in " + stopwatch.GetElapsedTime());
        }

        public async Task ExportToFile()
        {
            var database = await GetDatabase();
            var bytes = database.Export();

            var runtime = (IJSUnmarshalledRuntime)_jsRuntime;
            await runtime.ExportToFile(MoneizDownloadFileName, bytes);
            await _jsRuntime.SetValue(MoneizLocalStorageChangedName, false);
            RaiseDatabaseSaved();
        }

        public async Task Import(Database database)
        {
            await Save(database, new SaveOptions { IndicateDbChanged = false });
            if (_database != null)
            {
                _database.DatabaseChanged -= Database_DatabaseChanged;
            }

            _database = database;
            RaiseDatabaseChanged();
        }

        private void Database_DatabaseChanged(object sender, EventArgs e)
        {
            RaiseDatabaseChanged();
        }

        private void RaiseDatabaseSaved()
        {
            Console.WriteLine("DatabaseSaved");
            DatabaseSaved?.Invoke(this, EventArgs.Empty);
        }

        private void RaiseDatabaseChanged()
        {
            Console.WriteLine("DatabaseChanged");
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
        }

        public async ValueTask<bool> HasUnexportedChanges()
        {
            return (await _jsRuntime.GetValue<bool?>(MoneizLocalStorageChangedName)) ?? false;
        }

        public async ValueTask<DatabaseConfiguration> LoadConfiguration()
        {
            return (await _jsRuntime.GetValue<DatabaseConfiguration>(MoneizLocalStorageConfigurationName)) ?? new DatabaseConfiguration();
        }

        public ValueTask SetConfiguration(DatabaseConfiguration configuration)
        {
            return _jsRuntime.SetValue(MoneizLocalStorageConfigurationName, configuration);
        }

        private static HttpClient CreateClient(DatabaseConfiguration configuration)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", configuration.GitHubToken);
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
            httpClient.BaseAddress = new Uri("https://api.github.com");

            return httpClient;
        }

        public async Task ExportToGitHub()
        {
            var database = await GetDatabase();
            var bytes = database.Export();

            var configuration = await LoadConfiguration();

            using var httpClient = CreateClient(configuration);
            var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>(CacheBuster("user"));

            // https://developer.github.com/v3/repos/contents/#get-repository-content
            var url = "repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/contents/";
            var files = await httpClient.GetFromJsonAsync<GitHubContent[]>(CacheBuster(url));
            var file = files.FirstOrDefault(f => f.Name == MoneizDownloadFileName);

            // Check sha with persisted sha
            if (file != null && !configuration.GitHubSha.EqualsIgnoreCase(file.Sha))
            {
                Console.WriteLine($"GitHub dabatabase sha '{file.Sha}' is different from current db sha '{configuration.GitHubSha}'");
                if (await _confirmService.Confirm("The database on GitHub is not the same as the one synchronized on this machine. Do you want to overwrite it?") == false)
                    return;
            }

            var putResult = await httpClient.PutAsJsonAsync(url + MoneizDownloadFileName, new
            {
                message = "save db",
                content = bytes,
                sha = file?.Sha,
            });

            files = await httpClient.GetFromJsonAsync<GitHubContent[]>(CacheBuster(url));
            file = files.First(f => f.Name == MoneizDownloadFileName);

            // Save the blob sha
            configuration.GitHubSha = file.Sha;
            await SetConfiguration(configuration);

            await _jsRuntime.SetValue(MoneizLocalStorageChangedName, false);
            RaiseDatabaseSaved();
        }

        public async Task<bool> HasNewVersionOnGitHub()
        {
            if (await HasUnexportedChanges())
                return false;

            var configuration = await LoadConfiguration();
            using var httpClient = CreateClient(configuration);
            var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>(CacheBuster("user"));

            // https://developer.github.com/v3/repos/contents/#get-repository-content
            var url = "repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/contents/";
            var files = await SafeGetForJsonAsync<GitHubContent[]>(httpClient, CacheBuster(url));
            var file = files?.FirstOrDefault(f => f.Name == MoneizDownloadFileName);
            if (file != null && configuration.GitHubSha == null || !file.Sha.EqualsIgnoreCase(configuration.GitHubSha))
                return true;

            return false;
        }

        public async Task ImportFromGitHub(bool implicitLoad)
        {
            if (!implicitLoad && await HasUnexportedChanges())
            {
                if (await _confirmService.Confirm("You have unexported changes. Importing the database will destroy them. Do you want to proceed?") == false)
                    return;
            }

            var configuration = await LoadConfiguration();

            using var httpClient = CreateClient(configuration);
            var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>(CacheBuster("user"));

            // https://developer.github.com/v3/repos/contents/#get-repository-content
            var url = "repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/contents/";
            var files = await SafeGetForJsonAsync<GitHubContent[]>(httpClient, CacheBuster(url));
            var file = files?.FirstOrDefault(f => f.Name == MoneizDownloadFileName);
            if (file == null)
            {
                if (!implicitLoad)
                {
                    await _confirmService.Alert("No database found on GitHub");
                }
                else
                {
                    Console.WriteLine("No database found on GitHub");
                }

                return;
            }

            if (implicitLoad && (configuration.GitHubSha == null || file.Sha.EqualsIgnoreCase(configuration.GitHubSha)))
            {
                Console.WriteLine($"GitHub dabatabase sha '{file.Sha}' is equals to the current db sha '{configuration.GitHubSha}' => Do not import");
                return;
            }

            if (implicitLoad)
            {
                Console.WriteLine($"GitHub dabatabase sha '{file.Sha}' is different from current db sha '{configuration.GitHubSha}'");
                if (await _confirmService.Confirm("A new version of the database is available on GitHub. Do you want to load it?") == false)
                    return;
            }

            // Cannot use Download url for private repository
            // /repos/:owner/:repo/git/blobs/:file_sha
            var blob = await httpClient.GetFromJsonAsync<GitHubBlob>(CacheBuster("repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/git/blobs/" + file.Sha));
            var data = Convert.FromBase64String(blob.Content);
            var database = await Database.Load(data);
            await Import(database);
            configuration.GitHubSha = blob.Sha;
            await SetConfiguration(configuration);

        }

        private static async Task<T> SafeGetForJsonAsync<T>(HttpClient httpClient, string url) where T : class
        {
            try
            {
                return await httpClient.GetFromJsonAsync<T>(url);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task OpenGitHubRepository()
        {
            var configuration = await LoadConfiguration();

            using var httpClient = CreateClient(configuration);
            var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>("user");

            var url = "https://github.com/" + currentUser.Login + "/" + configuration.GitHubRepository;
            await _jsRuntime.OpenInTab(url);
        }

        public async ValueTask<bool> IsGitHubConfigured()
        {
            var configuration = await LoadConfiguration();
            return configuration.IsGitHubConfigured();
        }

        private static string CacheBuster(string url)
        {
            return QueryHelpers.AddQueryString(url, "z", new Random().Next().ToStringInvariant());
        }

        private sealed class GitHubUser
        {
            [JsonPropertyName("login")]
            public string Login { get; set; }
        }

        private sealed class GitHubContent
        {
            [JsonPropertyName("encoding")]
            public string Encoding { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }

            [JsonPropertyName("sha")]
            public string Sha { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        private sealed class GitHubBlob
        {
            [JsonPropertyName("encoding")]
            public string Encoding { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }

            [JsonPropertyName("sha")]
            public string Sha { get; set; }
        }
    }
}
