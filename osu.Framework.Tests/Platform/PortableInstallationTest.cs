// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NUnit.Framework;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class PortableInstallationTest
    {
        private readonly Storage startupStorage = new NativeStorage(RuntimeInfo.StartupDirectory);

        [Test]
        public void TestPortableInstall()
        {
            Assert.IsFalse(startupStorage.Exists(FrameworkConfigManager.FILENAME));

            using (var portable = new HeadlessGameHost(@"portable", portableInstallation: true))
            {
                portable.Run(new TestGame());
                Assert.AreEqual(startupStorage.GetFullPath(FrameworkConfigManager.FILENAME), portable.Storage.GetFullPath(FrameworkConfigManager.FILENAME));
            }

            // portable mode should write the configuration
            Assert.IsTrue(startupStorage.Exists(FrameworkConfigManager.FILENAME));

            // subsequent startups should detect the portable config and continue running in portable mode, even though it is not explicitly specified
            using (var portable = new HeadlessGameHost(@"portable"))
            {
                portable.Run(new TestGame());
                Assert.AreEqual(startupStorage.GetFullPath(FrameworkConfigManager.FILENAME), portable.Storage.GetFullPath(FrameworkConfigManager.FILENAME));
            }

            Assert.IsTrue(startupStorage.Exists(FrameworkConfigManager.FILENAME));
        }

        [Test]
        public void TestNonPortableInstall()
        {
            Assert.IsFalse(startupStorage.Exists(FrameworkConfigManager.FILENAME));

            using (var portable = new HeadlessGameHost(@"non-portable"))
            {
                portable.Run(new TestGame());
                Assert.AreEqual(startupStorage.GetFullPath(Path.Combine("headless", "non-portable", FrameworkConfigManager.FILENAME)), portable.Storage.GetFullPath(FrameworkConfigManager.FILENAME));
            }

            Assert.IsFalse(startupStorage.Exists(FrameworkConfigManager.FILENAME));
        }

        [TearDown]
        [SetUp]
        public void TearDown()
        {
            startupStorage.Delete(FrameworkConfigManager.FILENAME);
        }

        private class TestGame : Game
        {
            protected override void Update()
            {
                base.Update();
                Exit();
            }
        }
    }
}
