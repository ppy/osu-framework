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
        public void TestEnumerate()
        {
            var obj = new object();
            var list = new WeakList<object> { obj };

            Assert.That(list.Count(), Is.EqualTo(1));
            Assert.That(list, Does.Contain(obj));

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
        public void TestRemoveObjectsAtSides()
        {
            var objects = new List<object>
            {
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
            };

            var list = new WeakList<object>();
            foreach (var o in objects)
                list.Add(o);

            list.Remove(objects[0]);
            list.Remove(objects[1]);
            list.Remove(objects[4]);
            list.Remove(objects[5]);

            Assert.That(list.Count(), Is.EqualTo(2));
            Assert.That(list, Does.Contain(objects[2]));
            Assert.That(list, Does.Contain(objects[3]));
        }

        [Test]
        public void TestRemoveObjectsAtCentre()
        {
            var objects = new List<object>
            {
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
            };

            var list = new WeakList<object>();
            foreach (var o in objects)
                list.Add(o);

            list.Remove(objects[2]);
            list.Remove(objects[3]);

            Assert.That(list.Count(), Is.EqualTo(4));
            Assert.That(list, Does.Contain(objects[0]));
            Assert.That(list, Does.Contain(objects[1]));
            Assert.That(list, Does.Contain(objects[4]));
            Assert.That(list, Does.Contain(objects[5]));
        }

        [Test]
        public void TestAddAfterRemoveFromEnd()
        {
            var objects = new List<object>
            {
                new object(),
                new object(),
                new object(),
            };

            var newLastObject = new object();

            var list = new WeakList<object>();
            foreach (var o in objects)
                list.Add(o);

            list.Remove(objects[2]);
            list.Add(newLastObject);

            Assert.That(list.Count(), Is.EqualTo(3));
            Assert.That(list, Does.Contain(objects[0]));
            Assert.That(list, Does.Contain(objects[0]));
            Assert.That(list, Does.Not.Contain(objects[2]));
            Assert.That(list, Does.Contain(newLastObject));
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
        public void TestAddAfterClear()
        {
            var objects = new List<object>
            {
                new object(),
                new object(),
                new object(),
            };

            var newObject = new object();

            var list = new WeakList<object>();
            foreach (var o in objects)
                list.Add(o);

            list.Clear();
            list.Add(newObject);

            Assert.That(list.Count(), Is.EqualTo(1));
            Assert.That(list, Does.Contain(newObject));
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
            GC.TryStartNoGCRegion(10 * 1000000); // 10MB (should be enough)

            var (list, aliveObjects) = generateWeakList();

            GC.EndNoGCRegion();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (var obj in list)
            {
                if (!aliveObjects.Contains(obj))
                    Assert.Fail("Dead objects were iterated over.");
            }
        }

        [Test]
        public void TestDeadObjectsAreSkippedDuringEnumeration()
        {
            GC.TryStartNoGCRegion(10 * 1000000); // 10MB (should be enough)

            var (list, aliveObjects) = generateWeakList();

            using (var enumerator = list.GetEnumerator())
            {
                GC.EndNoGCRegion();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                while (enumerator.MoveNext())
                {
                    if (!aliveObjects.Contains(enumerator.Current))
                        Assert.Fail("Dead objects were iterated over.");
                }
            }
        }

        // This method is required for references to be released in DEBUG builds.
        private (WeakList<object> list, object[] aliveObjects) generateWeakList()
        {
            var objects = new List<object>
            {
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
                new object(),
            };

            var list = new WeakList<object>();
            foreach (var o in objects)
                list.Add(o);

            return (list, new[] { objects[1], objects[2], objects[4] });
        }
    }
}
