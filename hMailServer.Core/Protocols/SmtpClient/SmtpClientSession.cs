using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using hMailServer.Core.Entities;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.SMTP;

namespace hMailServer.Core.Protocols.SmtpClient
{
    public class SmtpClientSession : ISession
    {
        private IConnection _connection;
        private readonly SmtpServerSessionState _state = new SmtpServerSessionState();
        private readonly SmtpClientSessionConfiguration _configuration;
        private const int DataTransferMemoryBufferMaxSize = 1024*30;
        private readonly ILog _log;

        private MessageData _messageData;

        public List<DeliveryResult> DeliveryResult = new List<DeliveryResult>();

        public SmtpClientSession(ILog log, SmtpClientSessionConfiguration configuration, MessageData messageData)
        {
            _log = log;
            _configuration = configuration;

            _messageData = messageData;
        }

        public Protocol Protocol => Protocol.SMTPD;

        public async Task HandleConnection(IConnection connection)
        {
            _connection = connection;
            
            try
            {
                _connection.SetTimeout(_configuration.EnvelopeCommandTimeout);
                // Handle banner message
                var reply = await ReadUntilNewLine(_connection);

                // Send HELO
                await SendLine("HELO " + _configuration.HostName);
                reply = await ReadUntilNewLine(_connection);

                // Send MAIL FROM
                await SendLine("MAIL FROM:<" + _messageData.From + ">");
                reply = await ReadUntilNewLine(_connection);


                // Send RCPT TO
                foreach (var recipient in _messageData.Recipients)
                {
                    await SendLine("RCPT TO:<" + recipient + ">");
                    reply = await ReadUntilNewLine(_connection);
                }

                await SendLine("DATA");
                reply = await ReadUntilNewLine(_connection);

                // Send DATA
                await SendLine("DATA");

                //_connection.WriteBytes()

                await SendLine(".");
                reply = await ReadUntilNewLine(_connection);


                // Send Actual data
                await SendLine("QUIT");
                reply = await ReadUntilNewLine(_connection);
            }
            catch (DisconnectedException)
            {
                // Connection gone.
            }
        }

        public Task<string> ReadUntilNewLine(IConnection connection)
        {
            return connection.ReadStringUntil("\r\n");
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
