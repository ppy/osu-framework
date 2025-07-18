// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Platform;
using osu.Framework.Tests.IO;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public partial class IPCTest
    {
        [Test]
        public void TestNoPipeNameSpecifiedCoexist()
        {
            using (var host1 = new BackgroundGameHeadlessGameHost(@"server", new HostOptions { IPCPipeName = null }))
            using (var host2 = new HeadlessGameHost(@"client", new HostOptions { IPCPipeName = null }))
            {
                Assert.IsTrue(host1.IsPrimaryInstance, @"Host 1 wasn't able to bind");
                Assert.IsTrue(host2.IsPrimaryInstance, @"Host 2 wasn't able to bind");
            }
        }

        [Test]
        public void TestDifferentPipeNamesCoexist()
        {
            using (var host1 = new BackgroundGameHeadlessGameHost(@"server", new HostOptions { IPCPipeName = "test-app" }))
            using (var host2 = new HeadlessGameHost(@"client", new HostOptions { IPCPipeName = "test-app-2" }))
            {
                Assert.IsTrue(host1.IsPrimaryInstance, @"Host 1 wasn't able to bind");
                Assert.IsTrue(host2.IsPrimaryInstance, @"Host 2 wasn't able to bind");
            }
        }

        [Test]
        public void TestOneWay()
        {
            using (var server = new BackgroundGameHeadlessGameHost(@"server", new HostOptions { IPCPipeName = "test-app" }))
            using (var client = new HeadlessGameHost(@"client", new HostOptions { IPCPipeName = "test-app" }))
            {
                Assert.IsTrue(server.IsPrimaryInstance, @"Server wasn't able to bind");
                Assert.IsFalse(client.IsPrimaryInstance, @"Client was able to bind when it shouldn't have been able to");

                var serverChannel = new IpcChannel<Foobar>(server);
                var clientChannel = new IpcChannel<Foobar>(client);

                async Task waitAction()
                {
                    using (var received = new SemaphoreSlim(0))
                    {
                        serverChannel.MessageReceived += message =>
                        {
                            Assert.AreEqual("example", message.Bar);
                            // ReSharper disable once AccessToDisposedClosure
                            received.Release();
                            return null;
                        };

                        await clientChannel.SendMessageAsync(new Foobar { Bar = "example" }).ConfigureAwait(false);

                        if (!await received.WaitAsync(10000).ConfigureAwait(false))
                            throw new TimeoutException("Message was not received in a timely fashion");
                    }
                }

                Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
            }
        }

        [Test]
        public void TestTwoWay()
        {
            using (var server = new BackgroundGameHeadlessGameHost(@"server", new HostOptions { IPCPipeName = "test-app" }))
            using (var client = new HeadlessGameHost(@"client", new HostOptions { IPCPipeName = "test-app" }))
            {
                Assert.IsTrue(server.IsPrimaryInstance, @"Server wasn't able to bind");
                Assert.IsFalse(client.IsPrimaryInstance, @"Client was able to bind when it shouldn't have been able to");

                var serverChannel = new IpcChannel<Foobar, Foobar>(server);
                var clientChannel = new IpcChannel<Foobar, Foobar>(client);

                async Task waitAction()
                {
                    using (var received = new SemaphoreSlim(0))
                    {
                        serverChannel.MessageReceived += message =>
                        {
                            Assert.AreEqual("example", message.Bar);
                            // ReSharper disable once AccessToDisposedClosure
                            received.Release();

                            return new Foobar { Bar = "test response" };
                        };

                        var response = await clientChannel.SendMessageWithResponseAsync(new Foobar { Bar = "example" }).ConfigureAwait(false);

                        if (!await received.WaitAsync(10000).ConfigureAwait(false))
                            throw new TimeoutException("Message was not received in a timely fashion");

                        Assert.That(response?.Bar, Is.EqualTo("test response"));
                    }
                }

                Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
            }
        }

        [Test]
        public void TestIpcLegacyPortSupport()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using (var server = new BackgroundGameHeadlessGameHost(@"server", new HostOptions { IPCPort = 45356 }))
            using (var client = new HeadlessGameHost(@"client", new HostOptions { IPCPort = 45356 }))
#pragma warning restore CS0618 // Type or member is obsolete
            {
                Assert.IsTrue(server.IsPrimaryInstance, @"Server wasn't able to bind");
                Assert.IsFalse(client.IsPrimaryInstance, @"Client was able to bind when it shouldn't have been able to");

                var serverChannel = new IpcChannel<Foobar, object>(server);
                var clientChannel = new IpcChannel<Foobar, object>(client);

                async Task waitAction()
                {
                    using (var received = new SemaphoreSlim(0))
                    {
                        serverChannel.MessageReceived += message =>
                        {
                            Assert.AreEqual("example", message.Bar);
                            // ReSharper disable once AccessToDisposedClosure
                            received.Release();
                            return null;
                        };

                        await clientChannel.SendMessageAsync(new Foobar { Bar = "example" }).ConfigureAwait(false);

                        if (!await received.WaitAsync(10000).ConfigureAwait(false))
                            throw new TimeoutException("Message was not received in a timely fashion");
                    }
                }

                Assert.IsTrue(Task.Run(waitAction).Wait(10000), @"Message was not received in a timely fashion");
            }
        }

        private class Foobar
        {
            public string Bar;
        }
    }
}
