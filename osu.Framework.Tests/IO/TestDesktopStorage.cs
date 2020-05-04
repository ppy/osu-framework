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

                Assert.IsTrue(basePath.EndsWith(guid));

                Assert.Throws<ArgumentException>(() => storage.GetFullPath("../"));
                Assert.Throws<ArgumentException>(() => storage.GetFullPath(".."));
                Assert.Throws<ArgumentException>(() => storage.GetFullPath("./../"));

                Assert.AreEqual(Path.GetFullPath(Path.Combine(basePath, "sub", "test")) + Path.DirectorySeparatorChar, storage.GetFullPath("sub/test/"));
                Assert.AreEqual(Path.GetFullPath(Path.Combine(basePath, "sub", "test")), storage.GetFullPath("sub/test"));
            }
        }

        [Test]
        public void TestBasePathUpdatesOnSubStorages()
        {
            var guid = new Guid().ToString();

            const string subdirectory_name = "subdirectory";

            using (var storage = new TemporaryNativeStorage(guid))
            {
                var originalBasePath = storage.GetFullPath(string.Empty);

                var storage2 = storage.GetStorageForDirectory(subdirectory_name);
                Assert.That(storage.GetFullPath(subdirectory_name), Is.EqualTo(storage2.GetFullPath(string.Empty)));

                storage.SetBasePath(Path.Combine(originalBasePath, "test"));

                Assert.That(storage.GetFullPath(string.Empty), Is.Not.EqualTo(originalBasePath));
                Assert.That(storage.GetFullPath(subdirectory_name), Is.EqualTo(storage2.GetFullPath(string.Empty)));
            }
        }
    }
}
