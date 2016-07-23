using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using hMailServer.Core.IntegrationTests;
using hMailServer.Core.Logging;
using hMailServer.Core.Protocols.SMTP;
using NUnit.Framework;

namespace hMailServer.Core.StressTests.SMTP
{
    [TestFixture]
    public class SmtpVolumeTests
    {

        [Test]
        public void TestSendManyMessagesOnSeparateConnections()
        {
            Func<ISession> connectionFactory = () =>
                new SmtpServerSession(new InMemoryCommandHandler(), new NullLog(), new SmtpServerSessionConfiguration());

            var serverConfiguration = new ServerConfiguration
            {
                    IpAddress = IPAddress.Parse("127.0.0.1"),
                    Port = 25
                };

            var smtpServer = new Server(connectionFactory, new NullLog(), serverConfiguration);
            var runTask = smtpServer.RunAsync();

            var throttler = new SemaphoreSlim(initialCount: 5);

            var sendTasks = new List<Task>();

            for (int i = 0; i < 100; i++)
                sendTasks.Add(SendMessageThrottled(throttler, smtpServer.LocalEndpoint));

            Task.WaitAll(sendTasks.ToArray());
            

            var stopTask = smtpServer.StopAsync();

            Assert.IsTrue(stopTask.Wait(TimeSpan.FromMilliseconds(2000)));
            Assert.IsTrue(runTask.Wait(TimeSpan.FromMilliseconds(2000)));
        }

        [Test]
        public void TestSendManyMessagesOnSingleConnections()
        {
            Func<ISession> connectionFactory = () =>
                new SmtpServerSession(new InMemoryCommandHandler(), new NullLog(), new SmtpServerSessionConfiguration());

            var serverConfiguration = new ServerConfiguration
            {
                    IpAddress = IPAddress.Parse("127.0.0.1"),
                    Port = 25
                };

            var smtpServer = new Server(connectionFactory, new NullLog(), serverConfiguration);
            var runTask = smtpServer.RunAsync();
            
            using (var client = new SmtpClient(smtpServer.LocalEndpoint.Address.ToString(), smtpServer.LocalEndpoint.Port))
            {
                for (int i = 0; i < 50; i++)
                {
                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress("sender@example.com");
                        message.To.Add(new MailAddress("recipient1@example.com"));
                        message.To.Add(new MailAddress("recipient2@example.com"));
                        message.Body = "Test";
                        client.Send(message);
                    }
                }
            }

            var stopTask = smtpServer.StopAsync();

            Assert.IsTrue(stopTask.Wait(TimeSpan.FromMilliseconds(2000)));
            Assert.IsTrue(runTask.Wait(TimeSpan.FromMilliseconds(2000)));
        }

        private async Task SendMessageThrottled(SemaphoreSlim throttler, IPEndPoint endpoint)
        {
            await throttler.WaitAsync();

            try
            {
                using (var client = new SmtpClient(endpoint.Address.ToString(), endpoint.Port))
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress("sender@example.com");
                    message.To.Add(new MailAddress("recipient1@example.com"));
                    message.To.Add(new MailAddress("recipient2@example.com"));
                    message.Body = "Test";
                    await client.SendMailAsync(message);
                }
            }
            finally
            {
                throttler.Release();
            }

        }
    }
}
