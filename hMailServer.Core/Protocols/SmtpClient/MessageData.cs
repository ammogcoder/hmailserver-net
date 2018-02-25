using System.Collections.Generic;
using System.IO;

namespace hMailServer.Core.Protocols.SmtpClient
{
    public class MessageData
    {
        public string From { get; set; }
        public List<string> Recipients { get; set; }
        public Stream Data { get; set; }
    }
}
