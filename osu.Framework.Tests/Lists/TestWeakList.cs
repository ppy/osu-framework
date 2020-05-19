// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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

            Assert.That(list.Count(), Is.EqualTo(1));
            Assert.That(list, Does.Contain(obj));

            GC.KeepAlive(obj);
        }

        [Test]
        public void TestAddWeakReference()
        {
            var obj = new object();
            var weakRef = new WeakReference<object>(obj);
            var list = new WeakList<object> { weakRef };

            Assert.That(list.Contains(weakRef), Is.True);

            GC.KeepAlive(obj);
        }

        [Test]
        public void TestRemove()
        {
            var obj = new object();
            var list = new WeakList<object> { obj };

            list.Remove(obj);

            Assert.That(list.Count(), Is.Zero);
            Assert.That(list, Does.Not.Contain(obj));

            GC.KeepAlive(obj);
        }

        [Test]
        public void TestRemoveWeakReference()
        {
            var obj = new object();
            var weakRef = new WeakReference<object>(obj);
            var list = new WeakList<object> { weakRef };

            list.Remove(weakRef);

            Assert.That(list.Count(), Is.Zero);
            Assert.That(list.Contains(weakRef), Is.False);

            GC.KeepAlive(obj);
        }

        [Test]
        public void TestCountIsZeroAfterClear()
        {
            var obj = new object();
            var weakRef = new WeakReference<object>(obj);
            var list = new WeakList<object> { obj, weakRef };

            list.Clear();

            Assert.That(list.Count(), Is.Zero);
            Assert.That(list, Does.Not.Contain(obj));
            Assert.That(list.Contains(weakRef), Is.False);

            GC.KeepAlive(obj);
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

            Assert.That(count, Is.EqualTo(3));

            Assert.That(list, Does.Contain(obj));
            Assert.That(list, Does.Not.Contain(obj2));
            Assert.That(list, Does.Contain(obj3));

            GC.KeepAlive(obj);
            GC.KeepAlive(obj2);
            GC.KeepAlive(obj3);
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

                Assert.That(item, Is.Not.EqualTo(obj2));

                count++;
            }

            Assert.That(count, Is.EqualTo(2));

            GC.KeepAlive(obj);
            GC.KeepAlive(obj2);
            GC.KeepAlive(obj3);
        }

        [Test]
        public void TestDeadObjectsAreSkippedBeforeEnumeration()
        {
            var (list, alive) = generateWeakObjects();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (var obj in list)
            {
                if (!alive.Contains(obj))
                    Assert.Fail("Dead objects were iterated over.");
            }
        }

        [Test]
        public void TestDeadObjectsAreSkippedDuringEnumeration()
        {
            var (list, alive) = generateWeakObjects();

            GC.TryStartNoGCRegion(10000);

            using (var enumerator = list.GetEnumerator())
            {
                GC.EndNoGCRegion();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                while (enumerator.MoveNext())
                {
                    if (!alive.Contains(enumerator.Current))
                        Assert.Fail("Dead objects were iterated over.");
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
