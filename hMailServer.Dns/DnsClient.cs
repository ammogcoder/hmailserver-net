using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using hMailServer.Core.Dns;

namespace hMailServer.Dns
{
    public class DnsClient : IDnsClient
    {
        private readonly ARSoft.Tools.Net.Dns.DnsClient _dnsClient;

        public DnsClient()
        {
            _dnsClient = new ARSoft.Tools.Net.Dns.DnsClient(ARSoft.Tools.Net.Dns.DnsClient.GetLocalConfiguredDnsServers(), 10000);
        }

        public async Task<List<IPAddress>> ResolveMxIpAddressesAsync(string domainName)
        {
            var hostNames = await ResolveMxHostNamesAsync(domainName);

            var result = new List<IPAddress>();

            foreach (var hostName in hostNames)
            {
                // TODO: Query for IPV6 as well.
                var dnsMessage = await _dnsClient.ResolveAsync(DomainName.Parse(hostName), RecordType.A);

                if (IsFailedQuery(dnsMessage))
                {
                    // TODO: Throw specific type
                    throw new Exception($"Dns query for {domainName} failed.");
                }

                var aRecords =
                    dnsMessage.AnswerRecords.OfType<ARecord>();

                result.AddRange(from record in aRecords
                                select record.Address);
            }

            return result;
        }

        private async Task<List<string>> ResolveMxHostNamesAsync(string domainName)
        {
            var dnsMessage = await _dnsClient.ResolveAsync(DomainName.Parse(domainName), RecordType.Mx);

            if (IsFailedQuery(dnsMessage))
            {
                // TODO: Throw specific type
                throw new Exception($"Dns query for {domainName} failed.");
            }

            var mxRecordsByPreference = 
                dnsMessage.AnswerRecords.OfType<MxRecord>().OrderBy(item => item.Preference);

            var result =
                (from record in mxRecordsByPreference
                 select record.ExchangeDomainName.ToString().ToLowerInvariant()).Distinct().ToList();

            return result;
        }

        private bool IsFailedQuery(DnsMessage message)
        {
            return message == null || (message.ReturnCode != ReturnCode.NoError && message.ReturnCode != ReturnCode.NxDomain);
        }
    }
}
