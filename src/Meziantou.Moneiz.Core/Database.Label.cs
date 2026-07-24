namespace Meziantou.Moneiz.Core;

public partial class Database
{
    public IEnumerable<string> GetAllLabels()
    {
        return Transactions
            .Where(t => t.Labels is { Length: > 0 })
            .SelectMany(t => t.Labels!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.CurrentCultureIgnoreCase);
    }

    public void RenameLabel(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Label name cannot be empty.", nameof(newName));

        using (DeferEvents())
        {
            foreach (var transaction in Transactions.ToList())
            {
                if (transaction.Labels is null)
                    continue;

                var index = Array.IndexOf(transaction.Labels, oldName);
                if (index < 0)
                    continue;

                var newLabels = (string[])transaction.Labels.Clone();
                newLabels[index] = newName;

                // Deduplicate in case newName was already present
                transaction.Labels = newLabels.Distinct(StringComparer.Ordinal).ToArray();
                SaveTransaction(transaction);
            }
        }
    }

    public void DeleteLabel(string name)
    {
        using (DeferEvents())
        {
            foreach (var transaction in Transactions.ToList())
            {
                if (transaction.Labels is null)
                    continue;

                if (!transaction.Labels.Contains(name))
                    continue;

                var newLabels = transaction.Labels.Where(l => !string.Equals(l, name, StringComparison.Ordinal)).ToArray();
                transaction.Labels = newLabels.Length > 0 ? newLabels : null;
                SaveTransaction(transaction);
            }
        }
    }
}
