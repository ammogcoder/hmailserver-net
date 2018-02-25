using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hMailServer.Entities
{
    [Flags]
    public enum MessageFlags
    {
        Seen = 1,
        Deleted = 2,
        Flagged = 4,
        Answered = 8,
        Draft = 16,
        Recent = 32,
        VirusScan = 64
    };
}
