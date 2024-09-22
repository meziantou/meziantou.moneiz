using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.AspNetCore.Components.WebAssembly;
using Meziantou.Framework;
using Meziantou.Moneiz.Core;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Meziantou.Moneiz.Services;

public sealed class DatabaseProvider : IDisposable
{
    private const string MoneizLocalStorageDbName = "moneiz.db";
    private const string MoneizLocalStorageConfigurationName = "moneiz.configuration";
    private const string MoneizLocalStorageChangedName = "moneiz.dbchanged";

    private const string MoneizDownloadFileName = "moneiz.db";
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private Database _database;

    public event EventHandler DatabaseChanged;
    public event EventHandler DatabaseSaved;

    public void Dispose() => _semaphore.Dispose();

    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "false-positive")]
    public async Task<Database> GetDatabase()
    {
        if (_database is null)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_database is null)
                {
                    var configuration = await LoadConfiguration();
                    if (configuration.GitHubAutoLoad && !string.IsNullOrWhiteSpace(configuration?.GitHubToken))
                    {
                        await ImportFromGitHub(implicitLoad: true);
                    }

                    if (_database is null)
                    {
                        var result = await GlobalInterop.GetByteArrayValue(MoneizLocalStorageDbName);
                        if (result is not null)
                        {
                            _database = await Database.Load(result);
                        }

                        _database ??= new Database();
                    }

                    _database.DatabaseChanged += Database_DatabaseChanged;
                }
            }
            finally
            {
                _ = _semaphore.Release();
            }
        }

        return _database;
    }

    public Task Save()
    {
        if (_database is null)
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
        await GlobalInterop.SetValue(MoneizLocalStorageDbName, bytes);
        Console.WriteLine("Database blob saved in " + stopwatch.GetElapsedTime());

        stopwatch = ValueStopwatch.StartNew();
        await GlobalInterop.SetValue(MoneizLocalStorageChangedName, options.IndicateDbChanged);
        Console.WriteLine("Database changed saved in " + stopwatch.GetElapsedTime());

        stopwatch = ValueStopwatch.StartNew();
        RaiseDatabaseSaved();
        Console.WriteLine("Database changed notification handled in " + stopwatch.GetElapsedTime());
    }

    public async Task ExportToFile()
    {
        var database = await GetDatabase();
        var bytes = database.Export();

        await GlobalInterop.ExportToFile(MoneizDownloadFileName, bytes);
        await GlobalInterop.SetValue(MoneizLocalStorageChangedName, value: false);
        RaiseDatabaseSaved();
    }

    public async Task Import(Database database)
    {
        await Save(database, new SaveOptions { IndicateDbChanged = false });
        if (_database is not null)
        {
            _database.DatabaseChanged -= Database_DatabaseChanged;
        }

        _database = database;
        RaiseDatabaseChanged();
    }

    private void Database_DatabaseChanged(object sender, EventArgs e) => RaiseDatabaseChanged();

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

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public async Task<bool> HasLocalChanges() => (await GlobalInterop.GetValue<bool?>(MoneizLocalStorageChangedName)) ?? false;

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public async Task<DatabaseConfiguration> LoadConfiguration() => (await GlobalInterop.GetValue<DatabaseConfiguration>(MoneizLocalStorageConfigurationName)) ?? new DatabaseConfiguration();

    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public Task SetConfiguration(DatabaseConfiguration configuration) => GlobalInterop.SetValue(MoneizLocalStorageConfigurationName, configuration);

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
    private static HttpClient CreateClient(DatabaseConfiguration configuration)
    {
        var handler = new CacheBusterHandler(new DefaultBrowserOptionsMessageHandler(new HttpClientHandler())
        {
            DefaultBrowserRequestCache = BrowserRequestCache.NoStore,
            DefaultBrowserRequestMode = BrowserRequestMode.Cors,
            DefaultBrowserRequestCredentials = BrowserRequestCredentials.Omit,
        });
        var httpClient = new HttpClient(handler, disposeHandler: true);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", configuration.GitHubToken);
        _ = httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0");
        httpClient.BaseAddress = new Uri("https://api.github.com");

        return httpClient;
    }

    public async Task ExportToGitHub()
    {
        var database = await GetDatabase();
        var bytes = database.Export();

        var configuration = await LoadConfiguration();

        using var httpClient = CreateClient(configuration);
        var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>("user");

        // https://developer.github.com/v3/repos/contents/#get-repository-content
        var url = "repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/contents/";
        var files = await httpClient.GetFromJsonAsync<GitHubContent[]>(url);
        var file = files.FirstOrDefault(f => f.Name == MoneizDownloadFileName);

        // Check sha with persisted sha
        if (file is not null && !configuration.GitHubSha.EqualsIgnoreCase(file.Sha))
        {
            Console.WriteLine($"GitHub database sha '{file.Sha}' is different from current db sha '{configuration.GitHubSha}'");
            if (!GlobalInterop.Confirm("The database on GitHub is not the same as the one synchronized on this machine. Do you want to overwrite it?"))
                return;
        }

        var putResult = await httpClient.PutAsJsonAsync(url + MoneizDownloadFileName, new
        {
            message = "save db",
            content = bytes,
            sha = file?.Sha,
        });

        _ = putResult.EnsureSuccessStatusCode();

        file = (await putResult.Content.ReadFromJsonAsync<UpdateFileResult>()).Content;

        // Save the blob sha
        configuration.GitHubSha = file.Sha;
        await SetConfiguration(configuration);

        await GlobalInterop.SetValue(MoneizLocalStorageChangedName, value: false);
        RaiseDatabaseSaved();
    }

    public async Task<bool> HasNewVersionOnGitHub()
    {
        if (await HasLocalChanges())
            return false;

        var configuration = await LoadConfiguration();
        using var httpClient = CreateClient(configuration);
        var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>("user");

        // https://developer.github.com/v3/repos/contents/#get-repository-content
        var url = "repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/contents/";
        var files = await SafeGetForJsonAsync<GitHubContent[]>(httpClient, url);
        var file = files?.FirstOrDefault(f => f.Name == MoneizDownloadFileName);
        if ((file is not null && configuration.GitHubSha is null) || !file.Sha.EqualsIgnoreCase(configuration.GitHubSha))
            return true;

        return false;
    }

    public async Task ImportFromGitHub(bool implicitLoad)
    {
        if (!implicitLoad && await HasLocalChanges())
        {
            if (!GlobalInterop.Confirm("You have local changes. Importing the database will destroy them. Do you want to proceed?"))
                return;
        }

        var configuration = await LoadConfiguration();

        using var httpClient = CreateClient(configuration);
        var currentUser = await httpClient.GetFromJsonAsync<GitHubUser>("user");

        // https://developer.github.com/v3/repos/contents/#get-repository-content
        var url = "repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/contents/";
        var files = await SafeGetForJsonAsync<GitHubContent[]>(httpClient, url);
        var file = files?.FirstOrDefault(f => f.Name == MoneizDownloadFileName);
        if (file is null)
        {
            if (!implicitLoad)
            {
                GlobalInterop.Alert("No database found on GitHub");
            }
            else
            {
                Console.WriteLine("No database found on GitHub");
            }

            return;
        }

        if (implicitLoad && (configuration.GitHubSha is null || file.Sha.EqualsIgnoreCase(configuration.GitHubSha)))
        {
            Console.WriteLine($"GitHub database sha '{file.Sha}' is equals to the current db sha '{configuration.GitHubSha}' => Do not import");
            return;
        }

        if (implicitLoad)
        {
            Console.WriteLine($"GitHub database sha '{file.Sha}' is different from current db sha '{configuration.GitHubSha}'");
            if (!GlobalInterop.Confirm("A new version of the database is available on GitHub. Do you want to load it?"))
                return;
        }

        // Cannot use Download url for private repository
        // /repos/:owner/:repo/git/blobs/:file_sha
        var blob = await httpClient.GetFromJsonAsync<GitHubBlob>("repos/" + currentUser.Login + "/" + configuration.GitHubRepository + "/git/blobs/" + file.Sha);
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
        GlobalInterop.OpenInTab(url);
    }

    public async ValueTask<bool> IsGitHubConfigured()
    {
        var configuration = await LoadConfiguration();
        return configuration.IsGitHubConfigured();
    }

    private sealed class GitHubUser
    {
        [JsonPropertyName("login")]
        public string Login { get; set; }
    }

    private sealed class UpdateFileResult
    {
        [JsonPropertyName("content")]
        public GitHubContent Content { get; set; }
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

    private sealed class CacheBusterHandler : DelegatingHandler
    {

        public CacheBusterHandler()
        {
        }

        public CacheBusterHandler(HttpMessageHandler innerHandler) => InnerHandler = innerHandler;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri.ToString();
            if (uri.Contains('?', StringComparison.Ordinal))
            {
                uri += "&z=" + Uri.EscapeDataString(DateTime.UtcNow.Ticks.ToStringInvariant());
            }
            else
            {
                uri += "?z=" + Uri.EscapeDataString(DateTime.UtcNow.Ticks.ToStringInvariant());
            }

            request.RequestUri = new Uri(uri, UriKind.RelativeOrAbsolute);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
