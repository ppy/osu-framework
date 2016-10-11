using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Desktop.Platform;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Tests.Platform
{
    [TestFixture]
    public class HeadlessGameHostTest
    {
        private class Foobar
        {
            public string Bar;
        }
    
        [Test]
        public async Task TestIPC()
        {
            using (var server = new HeadlessGameHost())
            using (var client = new HeadlessGameHost())
            {
                server.Load();
                client.Load();

                Assert.IsTrue(server.IsPrimaryInstance);
                Assert.IsFalse(client.IsPrimaryInstance);

                var serverChannel = new IPCChannel<Foobar>(server);
                var clientChannel = new IPCChannel<Foobar>(client);
                bool messageReceived = false;
                serverChannel.MessageReceived += message =>
                {
                    messageReceived = true;
                    Assert.AreEqual("example", message.Bar);
                };
                await clientChannel.SendMessage(new Foobar { Bar = "example" });
                Thread.Sleep(10); // hacky, yes. This gives the server code time to process the message
                Assert.IsTrue(messageReceived);
            }
        }
    }
}