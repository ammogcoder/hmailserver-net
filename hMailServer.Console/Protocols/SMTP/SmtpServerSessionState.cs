using System.Collections.Generic;
using hMailServer.Entities;

namespace hMailServer.Protocols.SMTP
{
    public class SmtpServerSessionState
    {
        public string FromAddress { get; set; }
        public List<Recipient> Recipients = new List<Recipient>();

        public void HandleRset()
        {
            FromAddress = null;
            Recipients.Clear();
        }
    }
}
