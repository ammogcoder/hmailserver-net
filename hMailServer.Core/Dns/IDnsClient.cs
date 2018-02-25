using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace hMailServer.Core.Dns
{
    public interface IDnsClient
    {
        Task<List<IPAddress>> ResolveMxIpAddressesAsync(string domainName);
    }
}
