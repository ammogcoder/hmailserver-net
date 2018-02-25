using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using hMailServer.Entities;
using MySql.Data.MySqlClient;

namespace hMailServer.Repository.MySQL
{
    public class FolderRepository : IFolderRepository
    {
        private const int RootParentId = -1;

        private readonly string _connectionString;

        public FolderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Folder>> GetFolders(long accountId)
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                var folders =
                    await
                        sqlConnection.QueryAsync<Folder>(
                            "SELECT * FROM hm_imapfolders WHERE folderaccountid = @folderaccountid",
                            new
                            {
                                folderaccountid = accountId,
                            });

                return folders.ToList();
            }
        }

        public async Task<Folder> GetInbox(long accountId)
        {
            var allFolders = await GetFolders(accountId);

            return allFolders.SingleOrDefault(folder => folder.ParentId == RootParentId && folder.Name.Equals("Inbox", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
