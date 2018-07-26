using System;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using hMailServer.Core.IntegrationTests;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.SMTP;
using NUnit.Framework;
using hMailServer.Tests.Common;
using MimeKit;
using System.Diagnostics;

namespace hMailServer.Core.StressTests
{
    [TestFixture]
    public class SmtpMessageBodySizeTests
    {
        [Test]
        public void Sending_1K_MessageShouldSucceed()
        {
            SendMessageAndValidateContent(1024 * 1); 
        }

        [Test]
        public void Sending_100K_MessageShouldSucceed()
        {
            SendMessageAndValidateContent(1024 * 100);
        }


        [Test]
        public void Sending_1MB_MessageShouldSucceed()
        {
            SendMessageAndValidateContent(1024 * 1024 * 1);
        }

        [Test]
        public void Sending_10MB_MessageShouldSucceed()
        {
            SendMessageAndValidateContent(1024*1024*10);
        }       

        [Test]
        public void Sending_100MB_MessageShouldSucceed()
        {
            SendMessageAndValidateContent(1024 * 1024 * 100);
        }

        private static void SendMessageAndValidateContent(int attachmentSize)
        {
            var body = RandomStringGenerator.RandomString(attachmentSize);

            var attachmentToSend = body.ToAttachment();
            var commandHandler = SendMessageWithAttachment(attachmentToSend);

            var message = MimeMessage.Load(commandHandler.Body);

            var attachmentStream = new MemoryStream();

            var receivedAttachment = (MimePart)message.Attachments.First();

            receivedAttachment.Content.DecodeTo(attachmentStream);

            var bodyInReceivedAttachment = Encoding.ASCII.GetString(attachmentStream.ToArray());
            Assert.AreEqual(body.ToString(), bodyInReceivedAttachment);
        }


        private static InMemoryCommandHandler SendMessageWithAttachment(Attachment attachment)
        {
            var commandHandler = new InMemoryCommandHandler();

            Func<ISession> connectionFactory = () =>
                new SmtpServerSession(commandHandler, new NullLog(), new SmtpServerSessionConfiguration());

            var serverConfiguration = new ServerConfiguration();

            var smtpServer = new Server(connectionFactory, new NullLog(),  serverConfiguration);
            var runTask = smtpServer.RunAsync();
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using (var client = new SmtpClient("127.0.0.1", smtpServer.LocalEndpoint.Port))
            using (var message = new MailMessage())
            {
                message.From = new MailAddress("sender@example.com");
                message.To.Add(new MailAddress("recipient1@example.com"));
                message.To.Add(new MailAddress("recipient2@example.com"));
                message.Body = "Test";
                message.Attachments.Add(attachment);


                client.Send(message);
            }
            stopwatch.Stop();

            var stopTask = smtpServer.StopAsync();

            Assert.IsTrue(stopTask.Wait(TimeSpan.FromMilliseconds(2000)));
            Assert.IsTrue(runTask.Wait(TimeSpan.FromMilliseconds(2000)));

            Console.WriteLine("Sending time was {0}", stopwatch.Elapsed);

            return commandHandler;
        }
    }

}
