using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using hMailServer.Core;
using hMailServer.Entities;
using MySql.Data.MySqlClient;

namespace hMailServer.Repository.MySQL
{
    class MessageRepository : IMessageRepository
    {
        private readonly string _connectionString;
        private readonly string _dataDirectory;

        public MessageRepository(string connectionString, string dataDirectory)
        {
            _connectionString = connectionString;
            _dataDirectory = dataDirectory;
        }

        public async Task InsertAsync(Message message, Stream stream)
        {
            var messageFullPath = Path.Combine(_dataDirectory, Path.ChangeExtension(Guid.NewGuid().ToString(), ".eml"));

            message.Filename = Path.GetFileName(messageFullPath);

            using (var fileStream = File.Open(messageFullPath, FileMode.CreateNew))
            {
                stream.CopyTo(fileStream);
            }

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                
                var messageId = await sqlConnection.InsertAsync<long>(message);

                message.Id = messageId;
            }

            foreach (var recipient in message.Recipients)
            {
                recipient.MessageId = message.Id;

                await InsertAsync(recipient);
            }
        }

        public async Task InsertAsync(Message message)
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                var messageId = await sqlConnection.InsertAsync<long>(message);

                message.Id = messageId;
            }

            foreach (var recipient in message.Recipients)
            {
                recipient.MessageId = message.Id;

                await InsertAsync(recipient);
            }
        }

        public async Task UpdateAsync(Message message)
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                await sqlConnection.UpdateAsync(message);
            }
        }

        public async Task DeleteAsync(Message message)
        {
            if (message.AccountId > 0)
                throw new InvalidOperationException(string.Format("Use DeleteAsync(account, message)"));

            var filename = Path.Combine(_dataDirectory, message.Filename);

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                await sqlConnection.DeleteAsync(message);

                await DeleteRecipientsAsync(message, sqlConnection);
            }

            File.Delete(filename);
        }

        private static async Task DeleteRecipientsAsync(Message message, MySqlConnection sqlConnection)
        {
            foreach (var recipient in message.Recipients)
            {
                await sqlConnection.DeleteAsync(recipient);
            }
        }

        public async Task DeleteAsync(Account account, Message message)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var filename = GetMessageFullFileName(account, message);

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();
                await sqlConnection.DeleteAsync(message);

                await DeleteRecipientsAsync(message, sqlConnection);
            }

            File.Delete(filename);
        }

        public async Task<Message> GetMessageToDeliverAsync()
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                // TODO: MessageNextTryTime should be included in SQL

                var messages =
                    await sqlConnection.QueryAsync<Message>("SELECT * FROM hm_messages WHERE messagetype = 1 AND messagelocked = 0 LIMIT 1");
                var message = messages.SingleOrDefault();

                if (message != null)
                {
                    var recipients = await sqlConnection.QueryAsync<Recipient>(
                                            "SELECT * FROM hm_messagerecipients where recipientmessageid = @recipientmessageid",
                                                new
                                                {
                                                    recipientmessageid = message.Id
                                                });

                    foreach (var recipient in recipients)
                        message.Recipients.Add(recipient);
                }

                return message;
            }
        }

        public Task<Message> CreateAccountLevelMessageAsync(Message message, Account account, Folder folder)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            var clonedMessage = message.Clone();
            clonedMessage.Id = 0;
            clonedMessage.AccountId = account.Id;
            clonedMessage.FolderId = folder.Id;
            clonedMessage.Filename = Path.ChangeExtension(Guid.NewGuid().ToString(), ".eml");
            clonedMessage.Recipients = new List<Recipient>();

            var messageFileFullPath = GetMessageFullFileName(account, clonedMessage);
            var messageDirectory = Path.GetDirectoryName(messageFileFullPath);

            // TODO: Should be possible to do this asynchronously.
            Directory.CreateDirectory(messageDirectory);
            File.Copy(Path.Combine(_dataDirectory, message.Filename), messageFileFullPath);

            return Task.FromResult(clonedMessage);
        }

        public async Task<List<Message>> GetMessages(long accountId, long folderId)
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                var messages =
                    await
                        sqlConnection.QueryAsync<Message>(
                            "SELECT * FROM hm_messages WHERE messageaccountid = @messageaccountid AND messagefolderid = @messagefolderid",
                            new
                            {
                                messageaccountid = accountId,
                                messagefolderid = folderId
                            });

                return messages.ToList();
            }
        }

        public Stream GetMessageData(Account account, Message message)
        {
            var messageFileFullPath = GetMessageFullFileName(account, message);

            return File.OpenRead(messageFileFullPath);
        }

        public async Task DeleteRecipientAsync(Recipient recipient)
        {
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));

            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                await sqlConnection.DeleteAsync(recipient);
            }
        }

        public Stream GetMessageData(Message message)
        {
            var messageFullPath = Path.Combine(_dataDirectory, message.Filename);
            
            return File.OpenRead(messageFullPath);
        }

        private string GetMessageFullFileName(Account account, Message message)
        {
            var accountMessageDirectory = GetAccountMessageDirectory(account);

            var messageDirectory = Path.Combine(accountMessageDirectory, message.Filename.Substring(0, 2));
            var messageFileFullPath = Path.Combine(messageDirectory, message.Filename);
            return messageFileFullPath;
        }

        public async Task InsertAsync(Recipient messageRecipient)
        {
            using (var sqlConnection = new MySqlConnection(_connectionString))
            {
                sqlConnection.Open();

                var messageRecipientId = await sqlConnection.InsertAsync<long>(messageRecipient);

                messageRecipient.Id = messageRecipientId;
            }
        }

        private string GetAccountMessageDirectory(Account account)
        {
            var mailbox = EmailAddressParser.GetMailbox(account.Address);
            var domain = EmailAddressParser.GetDomain(account.Address);

            return Path.Combine(_dataDirectory, domain, mailbox);
        }
    }
}
