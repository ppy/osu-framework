// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Tests.IO;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class HeadlessGameHostTest
    {
        [Test]
        public void TestIpc()
        {
            using (var server = new BackgroundGameHeadlessGameHost(@"server", bindIPC: true))
            using (var client = new BackgroundGameHeadlessGameHost(@"client", bindIPC: true))
            {
                Assert.IsTrue(server.IsListeningIpc, @"Server wasn't able to bind");
                Assert.IsFalse(client.IsListeningIpc, @"Client was able to bind when it shouldn't have been able to");

                var serverChannel = new IpcChannel<Foobar>(server);
                var clientChannel = new IpcChannel<Foobar>(client);

                Action waitAction = () =>
                {
                    using (var received = new ManualResetEventSlim(false))
                    {
                        serverChannel.MessageReceived += message =>
                        {
                            Assert.AreEqual("example", message.Bar);
                            // ReSharper disable once AccessToDisposedClosure
                            received.Set();
                        };

                        clientChannel.SendMessageAsync(new Foobar { Bar = "example" }).Wait();

                        received.Wait();
                    }
                };

                Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
            }
        }

        [Test]
        public void TestMultipleInstancesAllowed()
        {
            testMultipleInstancesInternal(nameof(TestMultipleInstancesAllowed), true);
        }

        [Test]
        public void TestMultipleInstancesNotAllowed()
        {
            Assert.Throws<InvalidOperationException>(() => testMultipleInstancesInternal(nameof(TestMultipleInstancesNotAllowed), false));
        }

        private void testMultipleInstancesInternal(string gameName, bool allowMultipleInstances)
        {
            using (var host1 = new HeadlessGameHost(gameName, allowMultipleInstances))
            using (var host2 = new HeadlessGameHost(gameName, allowMultipleInstances))
            {
                host1.Run(new TestGame());
                host2.Run(new TestGame());
            }
        }

        private class TestGame : Game
        {
            protected override void Update()
            {
                base.Update();
                Exit();
            }
        }

        private class Foobar
        {
            public string Bar;
        }
    }
}
