// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using NUnit.Framework;
using osu.Framework.Platform;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestDesktopStorage
    {
        [Test]
        public void TestRelativePaths()
        {
            var guid = new Guid().ToString();
            var storage = new DesktopStorage(guid, null);

            var basePath = storage.GetFullPath(string.Empty);

            Assert.IsTrue(basePath.EndsWith(guid));

            Assert.Throws<ArgumentException>(() => storage.GetFullPath("../"));
            Assert.Throws<ArgumentException>(() => storage.GetFullPath(".."));
            Assert.Throws<ArgumentException>(() => storage.GetFullPath("./../"));

            Assert.AreEqual(Path.GetFullPath(Path.Combine(basePath, "sub", "test")) + Path.DirectorySeparatorChar, storage.GetFullPath("sub/test/"));
            Assert.AreEqual(Path.GetFullPath(Path.Combine(basePath, "sub", "test")), storage.GetFullPath("sub/test"));

            storage.DeleteDirectory(string.Empty);
        }
    }
}
