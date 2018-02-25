namespace hMailServer.Core.Entities
{
    public class DeliveryResult
    {
        public string Recipient { get;  }  
        public ReplyCodeSeverity ReplyCodeSeverity { get; }
        public string ResultMessage { get;  }

        public DeliveryResult(string recipient, ReplyCodeSeverity replyCodeSeverity, string resultMessage)
        {
            Recipient = recipient;
            ReplyCodeSeverity = replyCodeSeverity;
            ResultMessage = resultMessage;
        }


    }
}

