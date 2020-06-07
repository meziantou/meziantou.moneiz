using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Meziantou.Moneiz.Core
{
    public sealed partial class Database
    {
        private static readonly JsonSerializerOptions s_jsonOptions = CreateOptions();

        public event EventHandler? DatabaseChanged;

        [JsonPropertyName("a")]
        public IList<Account> Accounts { get; set; } = new List<Account>();

        [JsonPropertyName("b")]
        public IList<Currency> Currencies { get; set; } = new List<Currency>();

        [JsonPropertyName("c")]
        public IList<Category> Categories { get; set; } = new List<Category>();

        [JsonPropertyName("d")]
        public IList<Payee> Payees { get; set; } = new List<Payee>();

        [JsonPropertyName("e")]
        public IList<Transaction> Transactions { get; set; } = new List<Transaction>();

        private static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                WriteIndented = false,
                ReferenceHandling = ReferenceHandling.Preserve,
            };
        }

        public byte[] Export()
        {
            using var ms = new System.IO.MemoryStream();
            using (var compressedStream = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal))
            using (var writer = new Utf8JsonWriter(compressedStream))
            {
                JsonSerializer.Serialize(writer, this, s_jsonOptions);
            }

            return ms.ToArray();
        }

        public static async Task<Database> Import(byte[] value)
        {
            using var ms = new System.IO.MemoryStream(value);
            using var compressedStream = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);

            var db = await JsonSerializer.DeserializeAsync<Database>(compressedStream, s_jsonOptions);
            db.AssertNoDetachedReferences();
            return db;
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
            DatabaseChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
