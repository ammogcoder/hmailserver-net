using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.SMTP;

namespace hMailServer.Core.Protocols.POP3
{
    public class Pop3ServerSession : ISession
    {
        private IConnection _connection;
        private readonly IPop3ServerCommandHandler _commandHandler;
        private readonly Pop3ServerSessionState _state = new Pop3ServerSessionState();
        private readonly Pop3ServerSessionConfiguration _configuration;
        private const int DataTransferMemoryBufferMaxSize = 1024*30;
        private ILog _log;

        public Pop3ServerSession(IPop3ServerCommandHandler commandHandler, ILog log, Pop3ServerSessionConfiguration configuration)
        {
            _commandHandler = commandHandler;
            _configuration = configuration;
            _log = log;
        }

        public Protocol Protocol => Protocol.POP3D;

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

                    var command = POP3.CommandParser.ParseCommand(data);

                    if (!_state.IsCommandValid(command))
                    {
                        await SendCommandResult(new Pop3CommandReply(false, "Invalid command in current state"));
                        continue;
                    }

                    switch (command)
                    {
                        case Pop3Command.Capa:
                            await HandleCapa();
                            break;
                        case Pop3Command.Help:
                            await HandleHelp();
                            break;
                        case Pop3Command.User:
                            await HandleUser(data);
                            break;
                        case Pop3Command.Pass:
                            await HandlePass(data);
                            break;
                        case Pop3Command.Stat:
                            await HandleStat();
                            break;
                        case Pop3Command.List:
                            await HandleList();
                            break;
                        case Pop3Command.Uidl:
                            await HandleUidl();
                            break;
                        case Pop3Command.Retr:
                            await HandleRetr(data);
                            break;
                        case Pop3Command.Dele:
                            await HandleDele(data);
                            break;
                        case Pop3Command.Quit:
                            await HandleQuit();
                            throw new DisconnectedException();
                    }
                }
            }
            catch (DisconnectedException)
            {
                // Connection gone.
            }
        }

        private async Task HandleDele(string command)
        {
            var messageNumber = CommandParser.ParseDele(command);

            if (messageNumber.HasValue)
            {
                var commandResult = await _commandHandler.HandleDele(messageNumber.Value);
                await SendCommandResult(commandResult);
            }
            else
                await SendCommandResult(Pop3CommandReply.CreateNoSuchMessage());
        }

        private async Task HandleRetr(string command)
        {
            var messageIndex = CommandParser.ParseRetr(command);

            if (messageIndex.HasValue)
            {
                var stream = await _commandHandler.HandleRetr(messageIndex.Value);

                if (stream == null)
                {
                    await SendCommandResult(Pop3CommandReply.CreateNoSuchMessage()); 
                }
                else
                {
                    using (stream)
                    {
                        await _connection.WriteString("+OK\r\n");

                        // TODO: Add transmission period.
                        byte[] buf = new byte[4096];

                        int bytesRead = 0;
                        while ((bytesRead = stream.Read(buf, 0, buf.Length)) > 0)
                        {
                            if (bytesRead < buf.Length)
                            {
                                var reducedBuffer = new byte[bytesRead];
                                Buffer.BlockCopy(buf, 0, reducedBuffer, 0, bytesRead);
                                await _connection.WriteBytes(reducedBuffer);
                            }
                            else
                                await _connection.WriteBytes(buf);
                        }
                    }

                    await _connection.WriteString("\r\n.\r\n");
                }
            }
            else
            {
                await SendCommandResult(Pop3CommandReply.CreateNoSuchMessage());
            }
        }

        private async Task HandleUidl()
        {
            var commandResult = await _commandHandler.HandleUidl();

            await SendCommandResult(commandResult);
        }

        private async Task HandleList()
        {
            var commandResult = await _commandHandler.HandleList();

            await SendCommandResult(commandResult);
        }

        private async Task HandleStat()
        {
            var commandResult = await _commandHandler.HandleStat();

            await SendCommandResult(commandResult);
        }

        private async Task HandleHelp()
        {
            await SendCommandResult(new Pop3CommandReply(true, "Normal POP3 commands allowed"));
        }

        private async Task HandleUser(string command)
        {
            string username = CommandParser.ParseUser(command);

            var commandResult = await _commandHandler.HandleUser(username);

            if (commandResult.Success)
            {
                if (string.IsNullOrWhiteSpace(username))
                {
                    // TODO: Check message
                    await SendCommandResult(new Pop3CommandReply(false, "Invalid user name."));
                    return;
                }

                _state.Username = username;

                await SendCommandResult(new Pop3CommandReply(true, "Send your password."));
            }
            else
            {
                await SendCommandResult(commandResult);
            }
        }

        private async Task HandlePass(string command)
        {
            var password = CommandParser.ParsePass(command);

            if (string.IsNullOrWhiteSpace(password))
            {
                // TODO: Check message
                await SendCommandResult(new Pop3CommandReply(false, "Invalid password"));
                return;
            }

            _state.Password = password;

            var commandResult = await _commandHandler.HandlePass(_state.Username, _state.Password);

            if (commandResult.Success)
            {
                _state.IsLoggedOn = true;
            }

            await SendCommandResult(commandResult);
        }

       
        private async Task HandleCapa()
        {
            await SendCommandResult(new Pop3CommandReply(true, "CAPA list follows\r\nUSER\r\nUIDL\r\nTOP\r\n."));
        }

        private Task HandleStls()
        {
            throw new NotImplementedException();
        }

        private async Task HandleRset()
        {
            _state.Reset();

            await SendCommandResult(await _commandHandler.HandleRset());
        }

        private async Task HandleQuit()
        {
            await SendCommandResult(await _commandHandler.HandleQuit());
        }

        private Task SendBanner()
        {
            string banner = string.Format(CultureInfo.InvariantCulture, "+OK POP3");

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

            string result = commandResult.Success ? "+OK" : "-ERR";

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
