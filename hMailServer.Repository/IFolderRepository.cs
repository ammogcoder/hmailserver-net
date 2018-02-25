using System.Collections.Generic;
using System.Threading.Tasks;
using hMailServer.Entities;

namespace hMailServer.Repository
{
    public interface IFolderRepository
    {
        Task<List<Folder>> GetFolders(long accountId);
        Task<Folder> GetInbox(long accountId);
    }
}
