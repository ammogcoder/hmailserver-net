using System;
using System.Net.Mail;

namespace hMailServer.Core
{
    public static class EmailAddressParser
    {
        public static bool IsValidEmailAddress(string address)
        {
            try
            {
                new MailAddress(address);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }

        }

        public static bool IsValidDomainName(string domainName)
        {
            try
            {
                string exampleAddress = string.Format("{0}@{1}", "A", domainName);
                new MailAddress(exampleAddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }

        }

        public static string GetDomain(string emailAddress)
        {
            return new MailAddress(emailAddress).Host;
        }

        public static string GetMailbox(string emailAddress)
        {
            return new MailAddress(emailAddress).User;
        }
    }
}
