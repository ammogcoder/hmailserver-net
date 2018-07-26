using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient;
using DnsClient.Protocol;
using hMailServer.Core.Dns;

namespace hMailServer.Dns
{
    public class DnsClient : IDnsClient
    {
        
        public DnsClient()
        {
            
        }

        public async Task<List<IPAddress>> ResolveMxIpAddressesAsync(string domainName)
        {
            var lookupClient = new LookupClient();

            var hostNames = await ResolveMxHostNamesAsync(domainName);

            var result = new List<IPAddress>();

            foreach (var hostName in hostNames)
            {
                // TODO: Query for IPV6 as well.
                var dnsQueryResponse = await lookupClient.QueryAsync(hostName, QueryType.A);

                if (dnsQueryResponse.HasError)
                {
                    // TODO: Throw specific type
                    throw new Exception($"Dns query for {domainName} failed.");
                }

                var aRecords =
                    dnsQueryResponse.Answers.ARecords();

                result.AddRange(from record in aRecords
                                select record.Address);
            }

            return result;
        }

        private async Task<List<string>> ResolveMxHostNamesAsync(string domainName)
        {
            var lookupClient = new LookupClient();

            var dnsQueryResponse = await lookupClient.QueryAsync(domainName, QueryType.MX);

            if (dnsQueryResponse.HasError)
            {
                // TODO: Throw specific type
                throw new Exception($"Dns query for {domainName} failed.");
            }

            var mxRecordsByPreference =
                dnsQueryResponse.Answers.MxRecords().OrderBy(item => item.Preference);

            var result =
                (from record in mxRecordsByPreference
                 select record.Exchange.Original.ToString().ToLowerInvariant()).Distinct().ToList();

            return result;
        }

    }
}
