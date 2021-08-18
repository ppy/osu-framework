// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Framework.Tests.IO;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class HeadlessGameHostTest
    {
        [Test]
        public void TestGameHostExceptionDuringSetupHost()
        {
            using (var host = new ExceptionDuringSetupGameHost(nameof(TestGameHostExceptionDuringSetupHost)))
            {
                Assert.Throws<InvalidOperationException>(() => host.Run(new TestGame()));
            }
        }

        [Test]
        public void TestGameHostDisposalWhenNeverRun()
        {
            using (new TestRunHeadlessGameHost(nameof(TestGameHostDisposalWhenNeverRun), true))
            {
                // never call host.Run()
            }
        }

        [Test]
        public void TestIpc()
        {
            using (var server = new BackgroundGameHeadlessGameHost(@"server", true))
            using (var client = new BackgroundGameHeadlessGameHost(@"client", true))
            {
                Assert.IsTrue(server.IsPrimaryInstance, @"Server wasn't able to bind");
                Assert.IsFalse(client.IsPrimaryInstance, @"Client was able to bind when it shouldn't have been able to");

                var serverChannel = new IpcChannel<Foobar>(server);
                var clientChannel = new IpcChannel<Foobar>(client);

                void waitAction()
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
                }

                Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
            }
        }

        private class Foobar
        {
            public string Bar;
        }

        public class ExceptionDuringSetupGameHost : TestRunHeadlessGameHost
        {
            public ExceptionDuringSetupGameHost(string gameName)
                : base(gameName)
            {
            }

            protected override void SetupForRun()
            {
                base.SetupForRun();
                throw new InvalidOperationException();
            }
        }
    }
}
