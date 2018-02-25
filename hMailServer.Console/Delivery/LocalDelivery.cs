using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hMailServer.Core;
using hMailServer.Core.Entities;
using hMailServer.Core.Logging;
using hMailServer.Entities;
using hMailServer.Repository;

namespace hMailServer.Delivery
{
    public class LocalDelivery
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly ILog _log;

        public LocalDelivery(IAccountRepository accountRepository, IMessageRepository messageRepository, IFolderRepository folderRepository, ILog log)
        {
            _accountRepository = accountRepository;
            _messageRepository = messageRepository;
            _folderRepository = folderRepository;
            _log = log;
        }

        public async Task<List<DeliveryResult>> DeliverAsync(Message message, List<Recipient> recipients)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (message.Recipients == null)
                throw new ArgumentNullException(nameof(message.Recipients));

            var result = new List<DeliveryResult>();

            foreach (var localRecipient in recipients)
            {
                _log.LogInfo(new LogEvent()
                    {
                        EventType = LogEventType.Application,
                        LogLevel = LogLevel.Info,
                        Message = $"Delivering message from {message.From} to {localRecipient.Address}",
                        Protocol = "SMTPD",
                    });

                var localAccount = await _accountRepository.GetByIdAsync(localRecipient.AccountId);

                // TODO: Check quotas
                var inbox = await _folderRepository.GetInbox(localAccount.Id);

                var accountLevelMessage = await _messageRepository.CreateAccountLevelMessageAsync(message, localAccount, inbox);

                // TODO: Execute rules
                
                // TODO: Perform forwarding

                // TODO: Add Trace headers.

                // TODO: Etc

                accountLevelMessage.State = MessageState.Delivered;

                await _messageRepository.InsertAsync(accountLevelMessage);

                // Delete the recipient right away, so that if there is a crash we don't end up sending to this recipient again.
                await _messageRepository.DeleteRecipientAsync(localRecipient);

                result.Add(new DeliveryResult(localRecipient.Address, ReplyCodeSeverity.Positive, "Message delivered."));
            }

            return result;
        }
    }
}
