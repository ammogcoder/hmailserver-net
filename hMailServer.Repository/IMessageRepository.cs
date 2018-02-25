using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using hMailServer.Entities;

namespace hMailServer.Repository
{
    public interface IMessageRepository
    {
        Task InsertAsync(Message message, Stream stream);
        Task InsertAsync(Message message);
        Task UpdateAsync(Message message);
        Task DeleteAsync(Message message);

        Task DeleteAsync(Account account, Message message);

        Task<Message> GetMessageToDeliverAsync();
        Task<Message> CreateAccountLevelMessageAsync(Message message, Account account, Folder folder);

        Task<List<Message>> GetMessages(long accountId, long folderId);

        Stream GetMessageData(Message message);
        Stream GetMessageData(Account account, Message message);

        Task DeleteRecipientAsync(Recipient recipient);
    }
}
