// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Lists;

namespace osu.Framework.Tests.Lists
{
    [TestFixture]
    public class TestWeakList
    {
        [Test]
        public void TestAdd()
        {
            var obj = new object();
            var list = new WeakList<object> { obj };

            int count = 0;
            foreach (var unused in list)
                count++;

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void TestRemove()
        {
            var obj = new object();
            var list = new WeakList<object> { obj };

            list.Remove(obj);

            int count = 0;
            foreach (var unused in list)
                count++;

            Assert.That(count, Is.Zero);
        }

        [Test]
        public void TestDeadObjectsAreSkipped()
        {
            var (list, alive) = generateWeakObjects();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            int index = 0;
            foreach (var obj in list)
            {
                if (alive[index] != obj)
                    Assert.Fail("Dead objects were iterated over.");
                index++;
            }
        }

        private (WeakList<object> list, object[] alive) generateWeakObjects()
        {
            var allObjects = new[]
            {
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
            };

            var list = new WeakList<object>();

            foreach (var obj in allObjects)
                list.Add(obj);

            return (list, new[] { allObjects[1], allObjects[2], allObjects[4] });
        }
    }
}
