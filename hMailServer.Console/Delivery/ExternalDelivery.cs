using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hMailServer.Core;
using hMailServer.Core.Dns;
using hMailServer.Core.Entities;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.SmtpClient;
using hMailServer.Entities;
using hMailServer.Repository;

namespace hMailServer.Delivery
{
    public class ExternalDelivery
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IDnsClient _dnsClient;
        private readonly ILog _log;

        public ExternalDelivery(IMessageRepository messageRepository, IDnsClient dnsClient, ILog log)
        {
            _messageRepository = messageRepository;
            _dnsClient = dnsClient;
            _log = log;
        }

        public async Task<List<DeliveryResult>> DeliverAsync(Message message, List<Recipient> recipients)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (message.Recipients == null)
                throw new ArgumentNullException(nameof(message.Recipients));

            var commaSeparatedRecipientList = string.Join(", ", recipients.Select(item => item.Address));

            _log.LogInfo(new LogEvent()
                {
                    EventType = LogEventType.Application,
                    LogLevel = LogLevel.Info,
                    Message = $"Delivering message from {message.From} to {commaSeparatedRecipientList}",
                    Protocol = "SMTPD",
                });

            var recipientsByDomain =
                recipients.GroupBy(recipient => EmailAddressParser.GetDomain(recipient.Address)).Distinct().ToList();

            var result = new List<DeliveryResult>();

            foreach (var domainWithRecipients in recipientsByDomain)
            {
                var ipAddresses = await _dnsClient.ResolveMxIpAddressesAsync(domainWithRecipients.Key);

                var remainingRecipientsOnDomain = domainWithRecipients.Select(item => item.Address).ToList();

                foreach (var ipAddress in ipAddresses)
                {
                    var client = new TcpClient();
                    await client.ConnectAsync(ipAddress, 25);
                    
                    var connection = new Connection(client, CancellationToken.None);

                    using (var messageStream = _messageRepository.GetMessageData(message))
                    {
                        var messageData = new MessageData()
                            {
                                From = message.From,
                                Recipients = remainingRecipientsOnDomain,
                                Data = messageStream
                            };



                        var clientSession = new SmtpClientSession(_log, new SmtpClientSessionConfiguration(), messageData);
                        await clientSession.HandleConnection(connection);

                        foreach (var deliveryResult in clientSession.DeliveryResult)
                        {
                            var matchingRecipient =
                                recipients.Single(recipient => string.Equals(recipient.Address, deliveryResult.Recipient,
                                            StringComparison.InvariantCultureIgnoreCase));

                            switch (deliveryResult.ReplyCodeSeverity)
                            {
                                case ReplyCodeSeverity.Positive:
                                    // Delete the recipient right away, so that if there is a crash we don't end up sending to this recipient again.
                                    await _messageRepository.DeleteRecipientAsync(matchingRecipient);
                                    result.Add(deliveryResult);
                                    remainingRecipientsOnDomain.Remove(deliveryResult.Recipient);

                                    _log.LogInfo(new LogEvent()
                                        {
                                            EventType = LogEventType.Application,
                                            LogLevel = LogLevel.Info,
                                            Message = $"Message delivery from {message.From} to {deliveryResult.Recipient} completed",
                                            Protocol = "SMTPD",
                                        });

                                    break;

                                case ReplyCodeSeverity.PermanentNegative:
                                    // Let this recipient be deleted after we've submitted bounce message. This is delayed so that we don't
                                    // lose the information if there's a crash.
                                    result.Add(deliveryResult);
                                    remainingRecipientsOnDomain.Remove(deliveryResult.Recipient);

                                    _log.LogInfo(new LogEvent()
                                        {
                                            EventType = LogEventType.Application,
                                            LogLevel = LogLevel.Info,
                                            Message = $"Message delivery from {message.From} to {deliveryResult.Recipient} failed permanently: {deliveryResult.ResultMessage}",
                                            Protocol = "SMTPD",
                                        });

                                    break;
                                case ReplyCodeSeverity.TransientNegative:
                                    _log.LogInfo(new LogEvent()
                                        {
                                            EventType = LogEventType.Application,
                                            LogLevel = LogLevel.Info,
                                            Message = $"Message delivery from {message.From} to {deliveryResult.Recipient} failed temporarily: {deliveryResult.ResultMessage}",
                                            Protocol = "SMTPD",
                                        });
                                    break;
                            }
                        }
                    }

                    if (!remainingRecipientsOnDomain.Any())
                    {
                        // No more recipients remaining.
                        break;
                    }
                }
            }

            return result;
        }
    }
}
