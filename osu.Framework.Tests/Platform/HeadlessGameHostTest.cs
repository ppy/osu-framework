// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class HeadlessGameHostTest
    {
        [Test]
        public void TestIpc()
        {
            using (var server = new HeadlessGameHost(@"server", true))
            using (var client = new HeadlessGameHost(@"client", true))
            {
                var testGame1 = new TestGame();
                var testGame2 = new TestGame();

                try
                {
                    // ReSharper disable once AccessToDisposedClosure
                    Task.Run(() => server.Run(testGame1));

                    while (!testGame1.HasProcessed)
                        Thread.Sleep(10);

                    // ReSharper disable once AccessToDisposedClosure
                    Task.Run(() => client.Run(testGame2));

                    while (!testGame2.HasProcessed)
                        Thread.Sleep(10);

                    Assert.IsTrue(server.IsPrimaryInstance, @"Server wasn't able to bind");
                    Assert.IsFalse(client.IsPrimaryInstance, @"Client was able to bind when it shouldn't have been able to");

                    var serverChannel = new IpcChannel<Foobar>(server);
                    var clientChannel = new IpcChannel<Foobar>(client);

                    Action waitAction = () =>
                    {
                        bool received = false;
                        serverChannel.MessageReceived += message =>
                        {
                            Assert.AreEqual("example", message.Bar);
                            received = true;
                        };

                        clientChannel.SendMessageAsync(new Foobar { Bar = "example" }).Wait();

                        while (!received)
                            Thread.Sleep(1);
                    };

                    Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
                }
                finally
                {
                    testGame1.Exit();
                    testGame2.Exit();
                }
            }
        }

        private class Foobar
        {
            public string Bar;
        }

        private class TestGame : Game
        {
            public bool HasProcessed;

            protected override void Update()
            {
                base.Update();
                HasProcessed = true;
            }
        }
    }
}
