// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
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
        public void TestIpc()
        {
            using (var server = new HeadlessGameHost(@"server", true))
            using (var client = new HeadlessGameHost(@"client", true))
            {
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

                    clientChannel.SendMessage(new Foobar { Bar = "example" }).Wait();

                    while (!received)
                        Thread.Sleep(1);
                };

                Assert.IsTrue(waitAction.BeginInvoke(null, null).AsyncWaitHandle.WaitOne(1000),
                    @"Message was not received in a timely fashion");
            }
        }
    }
}