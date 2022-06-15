// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using NUnit.Framework;
using osu.Framework.Testing;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestDesktopStorage
    {
        [Test]
        public void TestRelativePaths()
        {
            string guid = Guid.NewGuid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                string basePath = storage.GetFullPath(string.Empty);

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
            string guid = Guid.NewGuid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    using var x = storage.GetStream("../test");
                });

                Assert.Throws<ArgumentException>(() => storage.GetStorageForDirectory("../"));
            }
        }

        [Test]
        public void TestGetSubDirectoryStorage()
        {
            string guid = Guid.NewGuid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                Assert.That(storage.GetStorageForDirectory("subdir").GetFullPath(string.Empty), Is.EqualTo(Path.Combine(storage.GetFullPath(string.Empty), "subdir")));
            }
        }

        [Test]
        public void TestGetEmptySubDirectoryStorage()
        {
            string guid = Guid.NewGuid().ToString();

            using (var storage = new TemporaryNativeStorage(guid))
            {
                Assert.That(storage.GetStorageForDirectory(string.Empty).GetFullPath(string.Empty), Is.EqualTo(storage.GetFullPath(string.Empty)));
            }
        }
    }
}
