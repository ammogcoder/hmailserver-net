using System.Threading.Tasks;
using hMailServer.Entities;

namespace hMailServer.Repository
{
    public interface IAccountRepository
    {
        Task<Account> GetByIdAsync(long id);
        Task<Account> GetByNameAsync(string address);
        Task<Account> ValidatePasswordAsync(string username, string password);
    }
}
