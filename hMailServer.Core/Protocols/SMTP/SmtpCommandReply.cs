using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hMailServer.Core.Protocols.SMTP
{
    public class SmtpCommandReply
    {
        public int Code { get; private set; }
        public string Message { get; private set; }

        public SmtpCommandReply(int code, string message)
        {
            Code = code;
            Message = message;
        }

        public bool IsPositive()
        {
            return Code >= 200 && Code <= 299;
        }

        public static SmtpCommandReply CreateDefault250Success()
        {
            return new SmtpCommandReply(250, "Ok.");
        }

        public static Task<SmtpCommandReply> CreateDefault250SuccessTask()
        {
            return Task.Run(() => CreateDefault250Success());
        }
    }
}
