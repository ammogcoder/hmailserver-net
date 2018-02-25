using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hMailServer.Core.Protocols.SMTP.Parsing
{
    public class MailFromParseResult
    {
        public string Address { get; set; }
        public string Parameters { get; set; }
    }
}
