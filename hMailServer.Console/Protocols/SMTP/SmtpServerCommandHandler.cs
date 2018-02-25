using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using hMailServer.Core.Protocols.SMTP;
using hMailServer.Entities;
using hMailServer.Repository;
using StructureMap;

namespace hMailServer.Protocols.SMTP
{
    public class SmtpServerCommandHandler : ISmtpServerCommandHandler
    {
        private readonly SmtpServerSessionState _state = new SmtpServerSessionState();

        private readonly Container _container;

        public SmtpServerCommandHandler(Container container)
        {
            _container = container;
        }

        public Task<SmtpCommandReply> HandleRset()
        {
            _state.HandleRset();
            return SmtpCommandReply.CreateDefault250SuccessTask();
        }

        public Task<SmtpCommandReply> HandleHelo(string hostName)
        {
            return SmtpCommandReply.CreateDefault250SuccessTask();
        }

        public Task<SmtpCommandReply> HandleEhlo(string hostName)
        {
            return SmtpCommandReply.CreateDefault250SuccessTask();
        }

        public Task<SmtpCommandReply> HandleMailFrom(string fromAddress)
        {
            var accountRepository = _container.GetInstance<IAccountRepository>();
            var account = accountRepository.GetByNameAsync(fromAddress).Result;

            bool isLocalAccount = account != null;

            _state.FromAddress = fromAddress;
            return SmtpCommandReply.CreateDefault250SuccessTask();
        }

        public Task<SmtpCommandReply> HandleRcptTo(string recipientAddress)
        {
            var accountRepository = _container.GetInstance<IAccountRepository>();
            var account = accountRepository.GetByNameAsync(recipientAddress).Result;
            
            var recipient = new Recipient
            {
                    AccountId = account != null ? account.Id : 0,
                    Address = recipientAddress,
                    OriginalAddress = recipientAddress
                };

            _state.Recipients.Add(recipient);
            return SmtpCommandReply.CreateDefault250SuccessTask();
        }

        public async Task<SmtpCommandReply> HandleData(Stream stream)
        {
            var message = new Message
                {
                    From = _state.FromAddress,
                    State = MessageState.Delivering,
                    Recipients = _state.Recipients,
                    Size = stream.Length
                };

            var messageRepository = _container.GetInstance<IMessageRepository>();
            await messageRepository.InsertAsync(message, stream);

            return SmtpCommandReply.CreateDefault250Success();
        }
    }
}
