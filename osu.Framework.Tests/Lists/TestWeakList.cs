// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        public void TestIterateWithRemove()
        {
            var obj = new object();
            var obj2 = new object();
            var obj3 = new object();

            var list = new WeakList<object> { obj, obj2, obj3 };

            int count = 0;

            foreach (var item in list)
            {
                if (count == 1)
                    list.Remove(item);
                count++;
            }

            Assert.AreEqual(3, count);
        }

        [Test]
        public void TestIterateWithRemoveSkipsInvalidated()
        {
            var obj = new object();
            var obj2 = new object();
            var obj3 = new object();

            var list = new WeakList<object> { obj, obj2, obj3 };

            int count = 0;

            foreach (var item in list)
            {
                if (count == 0)
                    list.Remove(obj2);

                Assert.AreNotEqual(obj2, item);

                count++;
            }

            Assert.AreEqual(2, count);
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

        [Test]
        public void TestAddedObjectIsContained()
        {
            var obj = new object();
            var list = new WeakList<object> { obj };

            Assert.That(list, Contains.Item(obj));
        }

        [Test]
        public void TestAddedWeakReferenceIsContained()
        {
            var obj = new object();
            var weakRef = new WeakReference<object>(obj);
            var list = new WeakList<object> { weakRef };

            Assert.That(list.Contains(weakRef), Is.True);
        }

        [Test]
        public void TestRemovedObjectsAreNotContained()
        {
            var obj = new object();
            var list = new WeakList<object> { obj };

            GC.TryStartNoGCRegion(10 * 1000000); // 10MB (should be enough)

            try
            {
                list.Remove(obj);
                Assert.That(list, Does.Not.Contain(obj));
            }
            finally
            {
                try
                {
                    GC.EndNoGCRegion();
                }
                catch
                {
                }
            }
        }

        [Test]
        public void TestRemovedWeakReferencesAreNotContained()
        {
            var obj = new object();
            var weakRef = new WeakReference<object>(obj);
            var list = new WeakList<object> { weakRef };

            GC.TryStartNoGCRegion(10 * 1000000); // 10MB (should be enough)

            try
            {
                list.Remove(weakRef);
                Assert.That(list, Does.Not.Contain(weakRef));
            }
            finally
            {
                try
                {
                    GC.EndNoGCRegion();
                }
                catch
                {
                }
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
