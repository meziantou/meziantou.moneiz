using System;
using System.IO;
using System.Threading.Tasks;

namespace Meziantou.Moneiz.Core
{
    public interface IDatabaseProvider
    {
        event EventHandler? DatabaseChanged;
        Task<Database> GetDatabase();
        Task Save();
        Task Export();
        Task Import(Stream stream);
    }
}
