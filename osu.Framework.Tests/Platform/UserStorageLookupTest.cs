// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class UserStorageLookupTest
    {
        private static readonly string path1 = Path.Combine(TestRunHeadlessGameHost.TemporaryTestDirectory, "path1");
        private static readonly string path2 = Path.Combine(TestRunHeadlessGameHost.TemporaryTestDirectory, "path2");

        private const string game_name = "test_game";

        [TearDown]
        public void TearDown()
        {
            try
            {
                File.Delete(path1);
                File.Delete(path2);
            }
            catch
            {
            }

            try
            {
                Directory.Delete(path1, true);
                Directory.Delete(path2, true);
            }
            catch
            {
            }
        }

        [Test]
        public void TestFirstBaseExisting()
        {
            Directory.CreateDirectory(path1);

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path1, StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestSecondBaseExistingStillPrefersFirst()
        {
            Directory.CreateDirectory(path2);

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path1, StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestSecondBaseUsedIfFirstFails()
        {
            // write a file so directory creation fails.
            File.WriteAllText(path1, "");

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path2, StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestFirstDataExisting()
        {
            Directory.CreateDirectory(Path.Combine(path1, game_name));

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path1, StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestSecondDataExisting()
        {
            Directory.CreateDirectory(Path.Combine(path2, game_name));

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path2, StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestPrefersFirstData()
        {
            Directory.CreateDirectory(Path.Combine(path1, game_name));
            Directory.CreateDirectory(Path.Combine(path2, game_name));

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path1, StringComparison.Ordinal));
            }
        }

        [Test]
        public void TestPrefersSecondDataOverFirstBase()
        {
            Directory.CreateDirectory(path1);
            Directory.CreateDirectory(Path.Combine(path2, game_name));

            using (var host = new StorageLookupHeadlessGameHost())
            {
                runHost(host);
                Assert.IsTrue(host.Storage.GetFullPath(string.Empty).StartsWith(path2, StringComparison.Ordinal));
            }
        }

        private static void runHost(StorageLookupHeadlessGameHost host)
        {
            TestGame game = new TestGame();
            game.Schedule(() => game.Exit());
            host.Run(game);
        }

        private class StorageLookupHeadlessGameHost : TestRunHeadlessGameHost
        {
            public StorageLookupHeadlessGameHost()
                : base(game_name, new HostOptions())
            {
            }

            public override IEnumerable<string> UserStoragePaths => new[]
            {
                path1,
                path2,
            };
        }
    }
}
