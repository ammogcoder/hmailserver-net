using hMailServer.Repository;

namespace hMailServer.Configuration
{
    public class ServiceConfiguration
    {
        public DatabaseConfiguration DatabaseConfiguration { get; set; }

        public string TempDirectory { get; set; }
        public string DataDirectory { get; set; }
    }
}
