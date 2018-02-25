using System;
using System.Configuration;
using System.IO;
using hMailServer.Application;
using hMailServer.Repository;
using hMailServer.Repository.MySQL;
using Microsoft.Win32;

namespace hMailServer.Configuration
{
    static class ServiceConfigurationReader
    {
        public static ServiceConfiguration Read()
        {
            string installLocation;

            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var subKey = baseKey.OpenSubKey("SOFTWARE\\hMailServer"))
            {
                if (subKey == null)
                    throw new InvalidOperationException("Unable to locate SOFTWARE\\hMailServer registry key");

                installLocation = (string) subKey.GetValue("InstallLocation", string.Empty, RegistryValueOptions.None);
            }

            var binDirectory = Path.Combine(installLocation, "Bin");
            var inifilePath = Path.Combine(binDirectory, "hMailServer.ini");

            var iniFile = new IniFile(inifilePath);

            var databaseType = iniFile.Read("Database", "Type");
            
            var serviceConfiguration = new ServiceConfiguration();

            switch (databaseType.ToLowerInvariant())
            {
                case "mysql":
                {
                    var username = iniFile.Read("Database", "Username");
                    var password = iniFile.Read("Database", "Password");
                    uint port = uint.Parse(iniFile.Read("Database", "Port"));
                    var server = iniFile.Read("Database", "Server");
                    var database = iniFile.Read("Database", "Database");

                    serviceConfiguration.DatabaseConfiguration = new DatabaseConfiguration
                        {
                            Database = database,
                            Password = password,
                            Port = port,
                            Server = server,
                            Username = username
                        };
                    break;
                }
                default:
                    throw new NotImplementedException(string.Format("Database type {0} is not supported.", databaseType));
            }

            serviceConfiguration.TempDirectory = iniFile.Read("Directories", "TempFolder");
            serviceConfiguration.DataDirectory = iniFile.Read("Directories", "DataFolder");

            return serviceConfiguration;
        }
    }
}
