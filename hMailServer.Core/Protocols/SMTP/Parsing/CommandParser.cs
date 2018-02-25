using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using hMailServer.Core.Protocols.POP3;
using hMailServer.Core.Protocols.SMTP.Parsing;

namespace hMailServer.Core.Protocols.SMTP
{
    internal class CommandParser
    {
        // TODO: Limit host name matching?
        private static readonly Regex HeloEhloRegex = new Regex(@"^((HELO)|(EHLO)) (.{1,253})$", RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, SmtpCommand> Commands = new Dictionary<string, SmtpCommand>();

        static CommandParser()
        {
            var values = Enum.GetValues(typeof(SmtpCommand));

            foreach (var value in values)
                Commands[value.ToString().ToLowerInvariant()] = (SmtpCommand)value;
        }

        public static SmtpCommand ParseCommand(string command)
        {
            command = command.ToLowerInvariant();

            var parts = command.Split(' ');

            if (parts.Length == 0)
                return SmtpCommand.Unknown;

            if (Commands.ContainsKey(parts[0]))
                return Commands[parts[0]];

            return SmtpCommand.Unknown;
        }

        public static string ParseHeloEhlo(string command)
        {
            var match = HeloEhloRegex.Match(command);

            if (!match.Success)
                return null;
            
            if (match.Groups.Count != 5)
                return null;

            return match.Groups[4].Value;
        }

        public static MailFromParseResult ParseMailFrom(string command)
        {
            string commandWithoutMailFrom = command.Substring(10);

            int parametersStartPosition = 0;

            int firstQuotePos = commandWithoutMailFrom.IndexOf("\"", StringComparison.InvariantCultureIgnoreCase);

            if (firstQuotePos >= 0)
            {
                int lastQuotePos = commandWithoutMailFrom.LastIndexOf("\"", StringComparison.InvariantCultureIgnoreCase);

                if (firstQuotePos == lastQuotePos)
                    return null;

                parametersStartPosition = commandWithoutMailFrom.IndexOf(" ", lastQuotePos, StringComparison.InvariantCultureIgnoreCase);
            }
            else
            {
                parametersStartPosition = commandWithoutMailFrom.IndexOf(" ", StringComparison.InvariantCultureIgnoreCase);
            }

            int emailAddressEndPosition = 0;

            if (parametersStartPosition > 0)
            {
                emailAddressEndPosition = parametersStartPosition;
            }
            else
            {
                emailAddressEndPosition = commandWithoutMailFrom.Length;
            }

            string emailAddress = commandWithoutMailFrom.Substring(0, emailAddressEndPosition);
            string parameters = parametersStartPosition >= 0
                ? commandWithoutMailFrom.Substring(parametersStartPosition)
                : "";

            emailAddress = emailAddress.Trim('<', '>', ' ');

            return new MailFromParseResult
                {
                    Address = emailAddress,
                    Parameters = parameters
                };
        }

        public static string ParseRcptTo(string command)
        {
            var value = command.Substring(8);
            value = value.Replace("<", "");
            value = value.Replace(">", "");

            return value.Trim();
        }

        public static string ParseEhlo(string data)
        {
            return ParseHeloEhlo(data);
        }
    }
}
