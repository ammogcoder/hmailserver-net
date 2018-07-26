using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.SMTP;
using NUnit.Framework;

namespace hMailServer.Core.IntegrationTests
{
    [TestFixture]
    public class SmtpServerTests
    {
        [Test]
        public void TestSmtpEndToEnd()
        {
            var commandHandler = new InMemoryCommandHandler();

            Func<ISession> connectionFactory = () =>
                new SmtpServerSession(commandHandler, new NullLog(), new SmtpServerSessionConfiguration());

            var serverConfiguration = new ServerConfiguration()
                {
                    IpAddress = IPAddress.Parse("127.0.0.1")
                };

            var smtpServer = new Server(connectionFactory, new NullLog(), serverConfiguration);
            var runTask = smtpServer.RunAsync();

            using (var message = new MailMessage())
            {
                message.From = new MailAddress("sender@example.com");
                message.To.Add(new MailAddress("recipient1@example.com"));
                message.To.Add(new MailAddress("recipient2@example.com"));
                message.Body = "Test";

                using (
                    var client = new SmtpClient(smtpServer.LocalEndpoint.Address.ToString(),
                        smtpServer.LocalEndpoint.Port))

                {
                    client.Send(message);
                }
            }

            var stopTask = smtpServer.StopAsync();

            Assert.IsTrue(stopTask.Wait(TimeSpan.FromMilliseconds(2000)));
            Assert.IsTrue(runTask.Wait(TimeSpan.FromMilliseconds(2000)));

            Assert.AreEqual("sender@example.com", commandHandler.MailFrom);
            Assert.AreEqual(2, commandHandler.Recipients.Count);
            Assert.AreEqual("recipient1@example.com", commandHandler.Recipients[0]);
            Assert.AreEqual("recipient2@example.com", commandHandler.Recipients[1]);

            var bodyStream = new MemoryStream();
            commandHandler.Body.CopyTo(bodyStream);
            string mailMessage = Encoding.UTF8.GetString(bodyStream.ToArray());
            var bodyStart = mailMessage.IndexOf("\r\n\r\n", StringComparison.InvariantCultureIgnoreCase);
            var body = mailMessage.Substring(bodyStart + 4);
            Assert.AreEqual("Test\r\n\r\n", body);
        }

        [Test]
        public void TestSmtpEndToEndWithSsl()
        {
            var commandHandler = new InMemoryCommandHandler();

            var certificate = new X509Certificate2(Resources.debugcert, "secret", X509KeyStorageFlags.Exportable);

            var smtpSessionConfiguration = new SmtpServerSessionConfiguration()
                {
                    SslCertificate = certificate
                };

            Func<ISession> connectionFactory = () =>
                new SmtpServerSession(commandHandler, new NullLog(), smtpSessionConfiguration);

            var serverConfiguration = new ServerConfiguration()
            {
                IpAddress = IPAddress.Parse("127.0.0.1")
            };


            var smtpServer = new Server(connectionFactory, new NullLog(), serverConfiguration);
            var runTask = smtpServer.RunAsync();

            using (var message = new MailMessage())
            {
                message.From = new MailAddress("sender@example.com");
                message.To.Add(new MailAddress("recipient1@example.com"));
                message.To.Add(new MailAddress("recipient2@example.com"));
                message.Body = "Test";

                using (var client = new SmtpClient(smtpServer.LocalEndpoint.Address.ToString(),
                        smtpServer.LocalEndpoint.Port))


                {
                    ServicePointManager.ServerCertificateValidationCallback = (sender, serverCertificate, chain, sslPolicyErrors) =>
                    {
                        var certificate2 = (X509Certificate2) serverCertificate;
                        return certificate2.Thumbprint == certificate.Thumbprint;
                    };

                    client.EnableSsl = true;
                    client.Send(message);
                }
            }

            var stopTask = smtpServer.StopAsync();

            Assert.IsTrue(stopTask.Wait(TimeSpan.FromMilliseconds(2000)));
            Assert.IsTrue(runTask.Wait(TimeSpan.FromMilliseconds(2000)));

            Assert.AreEqual("sender@example.com", commandHandler.MailFrom);
            Assert.AreEqual(2, commandHandler.Recipients.Count);
            Assert.AreEqual("recipient1@example.com", commandHandler.Recipients[0]);
            Assert.AreEqual("recipient2@example.com", commandHandler.Recipients[1]);

            var bodyStream = new MemoryStream();
            commandHandler.Body.CopyTo(bodyStream);

            string mailMessage = Encoding.UTF8.GetString(bodyStream.ToArray());
            var bodyStart = mailMessage.IndexOf("\r\n\r\n", StringComparison.InvariantCultureIgnoreCase);
            var body = mailMessage.Substring(bodyStart + 4);
            Assert.AreEqual("Test\r\n\r\n", body);
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
