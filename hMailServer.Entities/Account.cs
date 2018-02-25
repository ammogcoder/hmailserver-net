using System;


namespace hMailServer.Entities
{
    public class Account
    {
        public long Id { get; set; }
        public long DomainId { get; set; }
        public string Address { get; set; }
        public string Password { get; set; }
        public string ActiveDirectoryDomain { get; set; }
        public string ActiveDirectoryUsername { get; set; }
        public bool Enabled { get; set; }
        public bool IsActiveDirectoryAccount { get; set; }
        public long MaxSize { get; set; }

        public bool OutOfOfficeEnabled { get; set; }
        public string OutOfOfficeSubject { get; set; }
        public string OutOfOfficeMessage { get; set; }
        public bool OutOfOfficeExpires { get; set; }
        public DateTime OutOfOfficeExpireTime { get; set; }

        public AdminLevel AdminLevel { get; set; }
        public long PasswordEncryption { get; set; }

        public bool ForwardingEnabled { get; set; }
        public string ForwardAddress { get; set; }
        public bool KeepOriginalWhenForwarding { get; set; }

        public bool SignatureEnabled { get; set; }
        public string SignaturePlainText { get; set; }
        public string SignatureHtml { get; set; }

        public DateTime LastLogonTime { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
