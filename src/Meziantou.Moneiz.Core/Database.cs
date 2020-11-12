using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Meziantou.Moneiz.Core
{
    public sealed partial class Database
    {
        private int _deferedEventCount = 0;
        private bool _deferedEventCalled = false;

        public event EventHandler? DatabaseChanged;

        public Database()
        {
            Currencies = InitializeCurrencies();
        }

        [JsonPropertyName("a")]
        public IList<Account> Accounts { get; set; } = new List<Account>();

        [JsonIgnore]
        public IEnumerable<Account> VisibleAccounts => Accounts.Where(a => !a.Closed).Sort();

        [JsonIgnore]
        public IEnumerable<Account> ClosedAccounts => Accounts.Where(a => a.Closed).Sort();

        [JsonIgnore]
        public IReadOnlyList<Currency> Currencies { get; }

        [JsonPropertyName("c")]
        public IList<Category> Categories { get; set; } = new List<Category>();

        [JsonPropertyName("d")]
        public IList<Payee> Payees { get; set; } = new List<Payee>();

        [JsonPropertyName("e")]
        public IList<Transaction> Transactions { get; set; } = new List<Transaction>();

        [JsonPropertyName("f")]
        public IList<ScheduledTransaction> ScheduledTransactions { get; set; } = new List<ScheduledTransaction>();

        [JsonPropertyName("g")]
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        public byte[] Export()
        {
            using var ms = new MemoryStream();
            // Write version
            ms.WriteByte(2);

            using (var compressedStream = new GZipStream(ms, CompressionLevel.Fastest))
            using (var writer = new Utf8JsonWriter(compressedStream))
            {
                JsonSerializer.Serialize(writer, this, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                    WriteIndented = false,
                });
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
                using var compressedStream = new GZipStream(stream, CompressionMode.Decompress);
                using var textReader = new StreamReader(compressedStream);
                var json = await textReader.ReadToEndAsync();
                var db1 = JsonSerializer.Deserialize<V1.Database>(json, new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                });

                db = db1?.ToDatabase2();
            }
            else if (buffer[0] == 2)
            {
                using var compressedStream = new GZipStream(stream, CompressionMode.Decompress);
                using var textReader = new StreamReader(compressedStream);
                var json = await textReader.ReadToEndAsync();
                db = JsonSerializer.Deserialize<Database>(json);
            }
            else
            {
                throw new Exception($"database version '{buffer[0]}' not expected");
            }

            if (db == null)
                throw new Exception("database is null");

            db.ResolveReferences();
            db.AssertNoDetachedReferences();
            db.ProcessScheduledTransactions();
            return db;
        }

        private void ResolveReferences()
        {
            foreach (var payee in Payees)
            {
                payee.ResolveReferences(this);
            }

            foreach (var transaction in Transactions)
            {
                transaction.ResolveReferences(this);
            }

            foreach (var scheduledTransaction in ScheduledTransactions)
            {
                scheduledTransaction.ResolveReferences(this);
            }
        }

        private void AssertNoDetachedReferences()
        {
            foreach (var payee in Payees)
            {
                if (payee.DefaultCategory != null)
                {
                    if (!Categories.Any(c => ReferenceEquals(c, payee.DefaultCategory)))
                        throw new MoneizException($"Database is not valid: category of payee '{payee}' is not valid");
                }
            }

            foreach (var transaction in Transactions)
            {
                if (transaction.Account != null)
                {
                    if (!Accounts.Any(c => ReferenceEquals(c, transaction.Account)))
                        throw new MoneizException($"Database is not valid: account of transaction '{transaction.Id}' is not valid");
                }

                if (transaction.Category != null)
                {
                    if (!Categories.Any(c => ReferenceEquals(c, transaction.Category)))
                        throw new MoneizException($"Database is not valid: category of transaction '{transaction.Id}' is not valid");
                }

                if (transaction.Payee != null)
                {
                    if (!Payees.Any(c => ReferenceEquals(c, transaction.Payee)))
                        throw new MoneizException($"Database is not valid: payee of transaction '{transaction.Id}' is not valid");
                }

                if (transaction.LinkedTransaction != null)
                {
                    if (!ReferenceEquals(transaction.LinkedTransaction.LinkedTransaction, transaction))
                        throw new MoneizException($"Database is not valid: linked transaction of transaction '{transaction.Id}' is not valid");

                    if (!Transactions.Any(c => ReferenceEquals(c, transaction.LinkedTransaction)))
                        throw new MoneizException($"Database is not valid: linked transaction of transaction '{transaction.Id}' is not valid");
                }
            }
        }

        private static void AddOrReplace<T>(IList<T> items, T? existingItem, T newItem) where T : class
        {
            if (existingItem != null)
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
            if (_deferedEventCount == 0)
            {
                LastModifiedDate = DateTime.UtcNow;
                DatabaseChanged?.Invoke(this, EventArgs.Empty);
                _deferedEventCalled = false;
            }
            else
            {
                _deferedEventCalled = true;
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
                database._deferedEventCount++;
                _database = database;
            }

            public void Dispose()
            {
                _database._deferedEventCount--;
                if (_database._deferedEventCalled)
                {
                    _database.RaiseDatabaseChanged();
                }
            }
        }
    }
}
