using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Meziantou.Moneiz.Core;

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(Database), GenerationMode = JsonSourceGenerationMode.Default)]
internal sealed partial class DatabaseJsonContext : JsonSerializerContext
{
}

public sealed partial class Database
{
    private int _deferredEventCount;
    private bool _deferredEventCalled;

    public event EventHandler? DatabaseChanged;

    public Database() => Currencies = InitializeCurrencies();

    [JsonPropertyName("a")]
    public IList<Account> Accounts { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<Account> VisibleAccounts => Accounts.Where(a => !a.Closed).Sort();

    [JsonIgnore]
    public IEnumerable<Account> ClosedAccounts => Accounts.Where(a => a.Closed).Sort();

    [JsonIgnore]
    public IReadOnlyList<Currency> Currencies { get; }

    [JsonPropertyName("c")]
    public IList<Category> Categories { get; set; } = [];

    [JsonPropertyName("d")]
    public IList<Payee> Payees { get; set; } = [];

    [JsonPropertyName("e")]
    public IList<Transaction> Transactions { get; set; } = [];

    [JsonPropertyName("f")]
    public IList<ScheduledTransaction> ScheduledTransactions { get; set; } = [];

    [JsonPropertyName("g")]
    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Incremented every time the database is modified. It allows to detect the modifications made while an export was running.
    /// </summary>
    [JsonIgnore]
    public int Revision { get; private set; }

    public byte[] Export()
    {
        using var ms = new MemoryStream();
        // Write version
        ms.WriteByte(2);

        using (var compressedStream = new GZipStream(ms, CompressionLevel.Fastest))
        using (var writer = new Utf8JsonWriter(compressedStream))
        {
            JsonSerializer.Serialize(writer, this, DatabaseJsonContext.Default.Database);
        }

        return ms.ToArray();
    }

    public static async Task<Database> Load(byte[] value)
    {
        using var ms = new MemoryStream(value);
        return await Load(ms);
    }

    public static async Task<Database> Load(Stream stream)
    {
        var buffer = new byte[1];
        var count = await stream.ReadAsync(buffer.AsMemory());
        if (count != 1)
            throw new Exception("Cannot read file");

        Database? db;
        if (buffer[0] == 1)
        {
            throw new InvalidOperationException("database version 1 is not supported anymore");
        }
        else if (buffer[0] == 2)
        {
            await using var compressedStream = new GZipStream(stream, CompressionMode.Decompress);
            using var textReader = new StreamReader(compressedStream);
            var json = await textReader.ReadToEndAsync();
            db = JsonSerializer.Deserialize<Database>(json, DatabaseJsonContext.Default.Database);
        }
        else
        {
            throw new Exception($"database version '{buffer[0]}' not expected");
        }

        if (db is null)
            throw new Exception("database is null");

        var index = new DatabaseReferenceIndex(db);
        db.ResolveReferences(index);
        db.AssertNoDetachedReferences(index);
        db.ProcessScheduledTransactions();
        return db;
    }

    private void ResolveReferences(DatabaseReferenceIndex index)
    {
        foreach (var payee in Payees)
        {
            payee.ResolveReferences(index);
        }

        foreach (var transaction in Transactions)
        {
            transaction.ResolveReferences(index);
        }

        foreach (var scheduledTransaction in ScheduledTransactions)
        {
            scheduledTransaction.ResolveReferences(index);
        }
    }

    private void AssertNoDetachedReferences(DatabaseReferenceIndex index)
    {
        foreach (var payee in Payees)
        {
            if (payee.DefaultCategory is not null)
            {
                if (!index.Categories.Contains(payee.DefaultCategory))
                    throw new MoneizException($"Database is not valid: category of payee '{payee}' is not valid");
            }
        }

        foreach (var transaction in Transactions)
        {
            if (transaction.Account is not null)
            {
                if (!index.Accounts.Contains(transaction.Account))
                    throw new MoneizException($"Database is not valid: account of transaction '{transaction.Id}' is not valid");
            }

            if (transaction.Category is not null)
            {
                if (!index.Categories.Contains(transaction.Category))
                    throw new MoneizException($"Database is not valid: category of transaction '{transaction.Id}' is not valid");
            }

            if (transaction.Payee is not null)
            {
                if (!index.Payees.Contains(transaction.Payee))
                    throw new MoneizException($"Database is not valid: payee of transaction '{transaction.Id}' is not valid");
            }

            if (transaction.LinkedTransaction is not null)
            {
                if (!ReferenceEquals(transaction.LinkedTransaction.LinkedTransaction, transaction))
                    throw new MoneizException($"Database is not valid: linked transaction of transaction '{transaction.Id}' is not valid");

                if (!index.Transactions.Contains(transaction.LinkedTransaction))
                    throw new MoneizException($"Database is not valid: linked transaction of transaction '{transaction.Id}' is not valid");
            }
        }

        foreach (var scheduledTransaction in ScheduledTransactions)
        {
            if (scheduledTransaction.Account is not null)
            {
                if (!Accounts.Any(a => ReferenceEquals(a, scheduledTransaction.Account)))
                    throw new MoneizException($"Database is not valid: account of scheduled transaction '{scheduledTransaction.Id}' is not valid");
            }

            if (scheduledTransaction.CreditedAccount is not null)
            {
                if (!Accounts.Any(a => ReferenceEquals(a, scheduledTransaction.CreditedAccount)))
                    throw new MoneizException($"Database is not valid: credited account of scheduled transaction '{scheduledTransaction.Id}' is not valid");
            }
        }
    }

    private static void AddOrReplace<T>(IList<T> items, T? existingItem, T newItem) where T : class
    {
        if (existingItem is not null)
        {
            var index = items.IndexOf(existingItem);
            if (index >= 0)
            {
                items[index] = newItem;
                return;
            }
        }

        items.Add(newItem);
    }

    private static int GenerateId<T>(IEnumerable<T> items, Func<T, int> idSelector)
    {
        var max = 0;
        foreach (var item in items)
        {
            var id = idSelector(item);
            if (id > max)
            {
                max = id;
            }
        }

        return max + 1;
    }

    private void RaiseDatabaseChanged()
    {
        Revision++;
        if (_deferredEventCount == 0)
        {
            LastModifiedDate = DateTime.UtcNow;
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
            _deferredEventCalled = false;
        }
        else
        {
            _deferredEventCalled = true;
        }
    }

    public IDisposable DeferEvents()
    {
        return new DeferedEvents(this);
    }

    private sealed class DeferedEvents : IDisposable
    {
        private readonly Database _database;

        public DeferedEvents(Database database)
        {
            database._deferredEventCount++;
            _database = database;
        }

        public void Dispose()
        {
            _database._deferredEventCount--;
            if (_database._deferredEventCalled)
            {
                _database.RaiseDatabaseChanged();
            }
        }
    }
}
