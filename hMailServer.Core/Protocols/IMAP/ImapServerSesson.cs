using System;
using System.Globalization;
using System.Threading.Tasks;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.POP3;

namespace hMailServer.Core.Protocols.IMAP
{
    public class ImapServerSession : ISession
    {
        private IConnection _connection;
        private readonly IImapServerCommandHandler _commandHandler;
        private readonly ImapServerSessionState _state = new ImapServerSessionState();
        private readonly ImapServerSessionConfiguration _configuration;
        private const int DataTransferMemoryBufferMaxSize = 1024*30;
        private ILog _log;

        public ImapServerSession(IImapServerCommandHandler commandHandler, ILog log, ImapServerSessionConfiguration configuration)
        {
            _commandHandler = commandHandler;
            _configuration = configuration;
            _log = log;
        }

        public async Task HandleConnection(IConnection connection)
        {
            _connection = connection;

            try
            {
                await SendBanner();

                while (true)
                {
                    _connection.SetTimeout(_configuration.CommandTimeout);

                    var data = await ReadUntilNewLine(_connection);

                    _log.LogInfo(new LogEvent()
                        {
                            Message = data,
                            RemoteEndpoint = _connection.RemoteEndpoint,
                            SessionId = _connection.SessionId
                        });

                    var command = IMAP.CommandParser.ParseCommand(data);

                    if (!_state.IsCommandValid(command))
                    {
                        await SendCommandResult(new Pop3CommandReply(false, "Invalid command in current state"));
                        continue;
                    }

                    switch (command)
                    {
                        default:
                            throw new DisconnectedException();
                    }
                }
            }
            catch (DisconnectedException)
            {
                // Connection gone.
            }
        }

        public Protocol Protocol => Protocol.IMAPD;

        private Task HandleRset()
        {
            throw new NotImplementedException();
            //   await SendCommandResult(_commandHandler.HandleRset());
        }

        private Task HandleQuit()
        {
            throw new NotImplementedException();
            //   await SendCommandResult(_commandHandler.HandleQuit());
        }

        private Task SendBanner()
        {
            string banner = string.Format(CultureInfo.InvariantCulture, "220 {0} ESMTP", Environment.MachineName);

            return SendLine(banner);
        }

        public Task<string> ReadUntilNewLine(IConnection connection)
        {
            return connection.ReadStringUntil("\r\n");
        }

        private async Task SendCommandResult(Pop3CommandReply commandResult)
        {
            if (commandResult == null)
                throw new ArgumentException("commandResult");

            string result = commandResult.Success ? "+OK" : "+ERR";

            var message = $"{result} {commandResult.Message}";
            await SendLine(message);
        }

        private Task SendLine(string message)
        {
            _log.LogInfo(new LogEvent()
            {
                Message = message,
                RemoteEndpoint = _connection.RemoteEndpoint,
                SessionId = _connection.SessionId
            });

            return _connection.WriteString(message + "\r\n");
        }

        
    }

}
