using hMailServer.Configuration;
using hMailServer.Core;
using hMailServer.Core.Dns;
using hMailServer.Dns;
using hMailServer.Repository;
using hMailServer.Repository.MySQL;
using StructureMap;

namespace hMailServer.Application
{
    class DependencyRegistry : Registry
    {
        public DependencyRegistry(ServiceConfiguration serviceConfiguration)
        {
            var repositoryFactory = new RepositoryFactory(serviceConfiguration.DatabaseConfiguration, serviceConfiguration.DataDirectory);

            For<IAccountRepository>().Use(() => repositoryFactory.CreateAccountRepository());
            For<IMessageRepository>().Use(() => repositoryFactory.CreateMessageRepository());
            For<IFolderRepository>().Use(() => repositoryFactory.CreateFolderRepository());

            For<IDnsClient>().Use<Dns.DnsClient>();

            For<ILog>().Use<Log>();
        }
    }
}
