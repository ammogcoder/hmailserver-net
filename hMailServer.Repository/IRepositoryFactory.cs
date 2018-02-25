namespace hMailServer.Repository
{
   public interface IRepositoryFactory
   {
      IAccountRepository CreateAccountRepository();
      IMessageRepository CreateMessageRepository();
      IFolderRepository CreateFolderRepository();
   }
}
