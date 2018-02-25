using System.Collections.Generic;
using System.Linq;
using Dapper;
using hMailServer.Repository.RelationalShared;
using MySql.Data.MySqlClient;

namespace hMailServer.Repository.MySQL
{
    public class RepositoryFactory : IRepositoryFactory
    {
        static RepositoryFactory()
        {
            var mappings = TypeColumnMappings.Create();

            foreach (var mapping in mappings)
            {
                Dapper.SqlMapper.SetTypeMap(
                   mapping.Type,
                    new CustomPropertyTypeMap(
                        mapping.Type,
                        (type, columnName) =>
                        {
                            if (mapping.FieldNameByColumnName.ContainsKey(columnName))
                            {
                                string fieldName = mapping.FieldNameByColumnName[columnName];

                                var properties = type.GetProperties();

                                return properties.FirstOrDefault(item => item.Name == fieldName);
                            }

                            return null;
                        }));
            }

            var resolver = new CustomSimpleCrudResolver(mappings);

            SimpleCRUD.SetTableNameResolver(resolver);
            SimpleCRUD.SetColumnNameResolver(resolver);
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.MySQL);
        }

        private readonly string _connectionString;
        private readonly string _dataDirectory;

        public RepositoryFactory(DatabaseConfiguration databaseConfiguration, string dataDirectory)
        {
            var connectionStringBuilder = new MySqlConnectionStringBuilder();
            connectionStringBuilder.UserID = databaseConfiguration.Username;
            connectionStringBuilder.Password = databaseConfiguration.Password;
            connectionStringBuilder.Server = databaseConfiguration.Server;
            connectionStringBuilder.Database = databaseConfiguration.Database;
            connectionStringBuilder.Port = databaseConfiguration.Port;

            _connectionString = connectionStringBuilder.ToString();

            _dataDirectory = dataDirectory;
        }

        public IAccountRepository CreateAccountRepository()
        {
            return new AccountRepository(_connectionString);
        }

        public IMessageRepository CreateMessageRepository()
        {
            return new MessageRepository(_connectionString, _dataDirectory);
        }
        
        public IFolderRepository CreateFolderRepository()
        {
            return new FolderRepository(_connectionString);
        }
    }
}
