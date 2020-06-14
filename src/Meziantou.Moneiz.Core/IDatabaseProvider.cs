using System;
using System.Threading.Tasks;

namespace Meziantou.Moneiz.Core
{
    public interface IDatabaseProvider
    {
        event EventHandler? DatabaseChanged;
        event EventHandler? DatabaseSaved;

        Task<Database> GetDatabase();
        Task Save();
        Task ExportToFile();
        Task Import(Database database);
        ValueTask<bool> HasUnexportedChanges();
        ValueTask SetConfiguration(DatabaseConfiguration configuration);
        ValueTask<DatabaseConfiguration> LoadConfiguration();
        Task ExportToGitHub();
        Task ImportFromGitHub(bool implicitLoad);
    }
}
