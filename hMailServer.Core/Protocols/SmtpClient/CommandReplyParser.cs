using hMailServer.Core.Protocols.SMTP;

namespace hMailServer.Core.Protocols.SmtpClient
{
    internal class CommandReplyParser
    {
        public static SmtpCommandReply ParseCommandReply(string commandReply)
        {
            commandReply = commandReply.ToLowerInvariant();

            if (commandReply.Length < 3)
                return new SmtpCommandReply(0, commandReply);

            if (commandReply.Length == 3)
            {
                int code;

                if (int.TryParse(commandReply, out code))
                {
                    return new SmtpCommandReply(code, string.Empty);
                }
            }
            else
            {
                string potentialCode = commandReply.Substring(0, 3);

                int code;

                if (int.TryParse(potentialCode, out code))
                    return new SmtpCommandReply(code, commandReply.Substring(5));
            }

            return new SmtpCommandReply(0, "Unexpected reply: " + commandReply);
        }

    }
}
