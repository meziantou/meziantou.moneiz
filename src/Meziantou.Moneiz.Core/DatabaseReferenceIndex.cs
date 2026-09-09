namespace Meziantou.Moneiz.Core;

/// <summary>
/// Indexes the entities of a <see cref="Database"/> by id and by instance, so references can be resolved and validated
/// without scanning the whole collections for every entity.
/// </summary>
internal sealed class DatabaseReferenceIndex
{
    public DatabaseReferenceIndex(Database database)
    {
        Accounts = new EntityIndex<Account>(database.Accounts, item => item.Id);
        Categories = new EntityIndex<Category>(database.Categories, item => item.Id);
        Payees = new EntityIndex<Payee>(database.Payees, item => item.Id);
        Transactions = new EntityIndex<Transaction>(database.Transactions, item => item.Id);
    }

    public EntityIndex<Account> Accounts { get; }

    public EntityIndex<Category> Categories { get; }

    public EntityIndex<Payee> Payees { get; }

    public EntityIndex<Transaction> Transactions { get; }
}

internal sealed class EntityIndex<T> where T : class
{
    private readonly Dictionary<int, T> _itemsById = [];
    private readonly HashSet<object> _items = new(ReferenceEqualityComparer.Instance);

    public EntityIndex(IEnumerable<T> items, Func<T, int> idSelector)
    {
        foreach (var item in items)
        {
            // Keep the first item of a given id, so the lookup behaves like the sequential search it replaces
            _ = _itemsById.TryAdd(idSelector(item), item);
            _ = _items.Add(item);
        }
    }

    public T? GetById(int? id) => id is int value && _itemsById.TryGetValue(value, out var item) ? item : null;

    public bool Contains(T item) => _items.Contains(item);
}
