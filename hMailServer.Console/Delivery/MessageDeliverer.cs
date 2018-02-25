using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using hMailServer.Core;
using hMailServer.Core.Dns;
using hMailServer.Core.Entities;
using hMailServer.Core.Logging;
using hMailServer.Entities;
using hMailServer.Repository;
using MimeKit;
using StructureMap;

namespace hMailServer.Delivery
{
    class MessageDeliverer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationToken _cancellationToken;

        private readonly ILog _log;
        private readonly IMessageRepository _messageRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IDnsClient _dnsClient;

        public MessageDeliverer(IMessageRepository messageRepository, IAccountRepository accountRepository,  IDnsClient dnsClient, ILog log, IFolderRepository folderRepository)
        {
            _cancellationToken = _cancellationTokenSource.Token;

            _messageRepository = messageRepository;
            _accountRepository = accountRepository;
            _dnsClient = dnsClient;
            _log = log;
            _folderRepository = folderRepository;
        }

        public async Task RunAsync()
        {
            var messageRepository = _messageRepository;

            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var message = await messageRepository.GetMessageToDeliverAsync();

                if (message != null)
                {
                    try
                    {
                        // TODO: This should not be done using await. Let it return a task and call ContinueWith so that
                        // we can support parallell deliveries.
                        await DeliverMessageAsync(message);
                    }
                    catch (Exception ex)
                    {
                        var logEvent = new LogEvent()
                            {
                                EventType = LogEventType.Application,
                                LogLevel = LogLevel.Error,
                                Message = ex.Message,
                                Protocol = "SMTPD",
                            };

                        _log.LogException(logEvent, ex);
                    }
                    continue;
                }

                await Task.Delay(TimeSpan.FromSeconds(5), _cancellationToken);
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();

            // TODO: When what?
            await Task.WhenAll();
        }

        private async Task DeliverMessageAsync(Message message)
        {
            message.NumberOfDeliveryAttempts++;

            bool isLastAttempt = message.NumberOfDeliveryAttempts >= 3;

            var deliveryResults = new List<DeliveryResult>();

            try
            {
                var remainingRecipients = new List<Recipient>(message.Recipients);

                var localDelivery = new LocalDelivery(_accountRepository, _messageRepository, _folderRepository, _log);
                deliveryResults.AddRange(await localDelivery.DeliverAsync(message, remainingRecipients.Where(recipient => recipient.AccountId != 0).ToList()));

                var externalDelivery = new ExternalDelivery(_messageRepository, _dnsClient, _log);
                deliveryResults.AddRange(await externalDelivery.DeliverAsync(message, remainingRecipients.Where(recipient => recipient.AccountId == 0).ToList()));

                var failedRecipients =
                    deliveryResults.Where(result => result.ReplyCodeSeverity == ReplyCodeSeverity.PermanentNegative ||
                                                    (isLastAttempt && result.ReplyCodeSeverity == ReplyCodeSeverity.TransientNegative));

                await SubmitBounceMessageAsync(message, failedRecipients);

                var deliveryCompleted =
                    deliveryResults.Any(result => result.ReplyCodeSeverity == ReplyCodeSeverity.TransientNegative);

                if (isLastAttempt  || !deliveryCompleted)
                {
                    await _messageRepository.DeleteAsync(message);
                }
            }
            catch (Exception ex)
            {
                var logEvent = new LogEvent()
                    {
                        EventType = LogEventType.Application,
                        LogLevel = LogLevel.Error,
                        Protocol = "SMTPD",
                    };

                if (isLastAttempt)
                    logEvent.Message = "Failed delivering message due to an error. Giving up.";
                else
                    logEvent.Message = "Failed delivering message due to an error. Will retry later.";

                _log.LogException(logEvent, ex);

                if (isLastAttempt)
                {
                    await _messageRepository.DeleteAsync(message);
                }
                else
                {
                    await _messageRepository.UpdateAsync(message);
                }

            }
        }

        private async Task SubmitBounceMessageAsync(Message message, IEnumerable<DeliveryResult> failedRecipients)
        {
            if (string.IsNullOrWhiteSpace(message.From))
                return;

            if (IsMailerDaemonAddress(message.From))
                return;

            DateTimeOffset sent;
            string subject;

            using (var messageData = _messageRepository.GetMessageData(message))
            {
                var originialMimeMessage = MimeMessage.Load(messageData);

                sent = originialMimeMessage.Date;
                subject = originialMimeMessage.Subject;
            }

                // TODO: Dont' hardcode this.
            string bounceMessageBody = @"Your message did not reach some or all of the intended recipients.

   Sent: %MACRO_SENT%
   Subject: %MACRO_SUBJECT%

The following recipient(s)could not be reached:

%MACRO_RECIPIENTS%

hMailServer";

            bounceMessageBody = bounceMessageBody.Replace("%MACRO_SENT%", sent.ToString());
            bounceMessageBody = bounceMessageBody.Replace("%MACRO_SUBJECT", subject);

            var recipientList = new StringBuilder();

            foreach (var failedRecipient in failedRecipients)
            {
                recipientList.AppendFormat("Recipient: {0}\r\n", failedRecipient.Recipient);
                recipientList.AppendFormat("Message: {1}\r\n", failedRecipient.ResultMessage);
                recipientList.AppendLine();
            }

            bounceMessageBody = bounceMessageBody.Replace("%MACRO_RECIPIENTS%", recipientList.ToString());

            var mimeMessage = new MimeMessage();
            // TODO: Sender address should be generated off domain name.
            mimeMessage.From.Add(new MailboxAddress("", "mailer-daemon@" + Environment.MachineName));
            mimeMessage.To.Add(new MailboxAddress("", message.From));

            // TODO: Don't hard-code
            mimeMessage.Subject = "Delivery failure";

            mimeMessage.Body = new TextPart("plain")
                {
                    Text = bounceMessageBody
                };


            // TODO: Don't hardcode buffer size.
            using (var mailStream = new MemoryStreamWithFileBacking(10000))
            {
                mimeMessage.WriteTo(mailStream, CancellationToken.None);

                var bounceMessage = new Message()
                    {
                        Recipients = new List<Recipient>()
                        {
                            new Recipient()
                            {
                                Address = message.From,
                                OriginalAddress = message.From
                            }
                        },
                        From = "mailer-daemon" + Environment.MachineName,
                        Size = mailStream.Length,
                        State = MessageState.Delivering,
                    };

                await _messageRepository.InsertAsync(bounceMessage);
            }
                
            throw new NotImplementedException();

        }

        private bool IsMailerDaemonAddress(string address)
        {
            var mailbox = EmailAddressParser.GetMailbox(address);

            return mailbox.Equals("MAILER-DAEMON", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
