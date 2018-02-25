using System.Threading.Tasks;

namespace hMailServer.Core.Protocols.POP3
{
    public class Pop3CommandReply
    {
        public bool Success { get; private set; }
        public string Message { get; private set; }

        public Pop3CommandReply(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static Pop3CommandReply CreateDefaultSuccess()
        {
            return new Pop3CommandReply(true, "OK");
        }

        public static Task<Pop3CommandReply> CreateDefaultSuccessTask()
        {
            return Task.Run(() => CreateDefaultSuccess());
        }

        public static Pop3CommandReply CreateNoSuchMessage()
        {
            return new Pop3CommandReply(false, "No such message");
        }
    }
}
