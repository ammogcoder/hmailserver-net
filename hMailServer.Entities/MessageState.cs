using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hMailServer.Entities
{
    public enum MessageState
    {
        Unknown = 0,
        Delivering = 1,
        Delivered = 2
    }
}
