using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace hMailServer.Core.Protocols.SmtpClient
{
    public class SmtpClientSessionConfiguration
    {
        public TimeSpan EnvelopeCommandTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan DataCommandTimeout { get; set; } = TimeSpan.FromMinutes(10);
        public string HostName { get; set; } = Environment.MachineName;

        public string TempDirectory { get; set; } = Path.GetTempPath();
    }
}
