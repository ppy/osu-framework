// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using NUnit.Framework;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestDesktopStorage
    {
        [Test]
        public void TestRelativePaths()
        {
            var guid = new Guid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                var basePath = storage.GetFullPath(string.Empty);

                Assert.IsTrue(basePath.EndsWith(guid, StringComparison.Ordinal));

                Assert.Throws<ArgumentException>(() => storage.GetFullPath("../"));
                Assert.Throws<ArgumentException>(() => storage.GetFullPath(".."));
                Assert.Throws<ArgumentException>(() => storage.GetFullPath("./../"));

                Assert.AreEqual(Path.GetFullPath(Path.Combine(basePath, "sub", "test")) + Path.DirectorySeparatorChar, storage.GetFullPath("sub/test/"));
                Assert.AreEqual(Path.GetFullPath(Path.Combine(basePath, "sub", "test")), storage.GetFullPath("sub/test"));
            }
        }

        [Test]
        public void TestAttemptEscapeRoot()
        {
            var guid = new Guid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                Assert.Throws<ArgumentException>(() => storage.GetStream("../test"));
                Assert.Throws<ArgumentException>(() => storage.GetStorageForDirectory("../"));
            }
        }

        [Test]
        public void TestGetSubDirectoryStorage()
        {
            var guid = new Guid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                Assert.That(storage.GetStorageForDirectory("subdir").GetFullPath(string.Empty), Is.EqualTo(Path.Combine(storage.GetFullPath(string.Empty), "subdir")));
            }
        }

        [Test]
        public void TestGetEmptySubDirectoryStorage()
        {
            var guid = new Guid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                Assert.That(storage.GetStorageForDirectory(string.Empty).GetFullPath(string.Empty), Is.EqualTo(storage.GetFullPath(string.Empty)));
            }
        }
    }
}
