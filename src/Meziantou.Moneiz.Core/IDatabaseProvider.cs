using System.Threading.Tasks;

namespace Meziantou.Moneiz.Core
{
    public interface IDatabaseProvider
    {
        Task<Database> GetDatabase();
        Task Save();
    }
}