// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TripleBufferTest
    {
        [Test]
        public void TestWriteOnly()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 1000; i++)
            {
                using (tripleBuffer.GetForWrite())
                {
                }
            }
        }

        [Test]
        public void TestReadOnly()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer, Is.Null);
        }

        [Test]
        public void TestSameBufferIsNotWrittenTwiceInRowNoContestation()
        {
            var tripleBuffer = createWithIDsMatchingIndices();

            using (var write = tripleBuffer.GetForWrite())
                Assert.That(write.Object?.ID, Is.EqualTo(0));

            // buffer 0: waiting for read
            // buffer 1: old
            // buffer 2: old

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer?.Object?.ID, Is.EqualTo(0));

            // buffer 0: last read
            // buffer 1: old
            // buffer 2: old

            using (var write = tripleBuffer.GetForWrite())
                Assert.That(write.Object?.ID, Is.EqualTo(1));

            // buffer 0: last read
            // buffer 1: waiting for read
            // buffer 2: old

            using (var write = tripleBuffer.GetForWrite())
                Assert.That(write.Object?.ID, Is.EqualTo(2));

            // buffer 0: last read
            // buffer 1: old
            // buffer 2: waiting for read

            using (var write = tripleBuffer.GetForWrite())
                Assert.That(write.Object?.ID, Is.EqualTo(1));

            // buffer 0: last read
            // buffer 1: waiting for read
            // buffer 2: old

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer?.Object?.ID, Is.EqualTo(1));

            // buffer 0: old
            // buffer 1: last read
            // buffer 2: old
        }

        [Test]
        public void TestSameBufferIsNotWrittenTwiceInRowContestation()
        {
            var tripleBuffer = createWithIDsMatchingIndices();

            // Test with first write in use during second.
            using (var write = tripleBuffer.GetForWrite())
                Assert.That(write.Object?.ID, Is.EqualTo(0));

            // buffer 0: waiting for read
            // buffer 1: old
            // buffer 2: old

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(0));

                // buffer 0: reading
                // buffer 1: old
                // buffer 2: old

                using (var write = tripleBuffer.GetForWrite())
                    Assert.That(write.Object?.ID, Is.EqualTo(1));

                // buffer 0: reading
                // buffer 1: waiting for read
                // buffer 2: old

                using (var write = tripleBuffer.GetForWrite())
                    Assert.That(write.Object?.ID, Is.EqualTo(2));

                // buffer 0: reading
                // buffer 1: old
                // buffer 2: waiting for read
            }

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(2));

                // buffer 0: old
                // buffer 1: old
                // buffer 2: reading

                using (var write = tripleBuffer.GetForWrite())
                    Assert.That(write.Object?.ID, Is.EqualTo(0));

                // buffer 0: waiting for read
                // buffer 1: old
                // buffer 2: reading

                using (var write = tripleBuffer.GetForWrite())
                    Assert.That(write.Object?.ID, Is.EqualTo(1));

                // buffer 0: old
                // buffer 1: waiting for read
                // buffer 2: reading

                using (var write = tripleBuffer.GetForWrite())
                    Assert.That(write.Object?.ID, Is.EqualTo(0));

                // buffer 0: waiting for read
                // buffer 1: old
                // buffer 2: reading
            }

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(0));
                // buffer 0: reading
                // buffer 1: old
                // buffer 2: old
            }
        }

        [Test]
        public void TestSameBufferIsNotWrittenTwiceInRowContestation2()
        {
            var tripleBuffer = createWithIDsMatchingIndices();

            using (var write = tripleBuffer.GetForWrite())
                Assert.That(write.Object?.ID, Is.EqualTo(0));

            // buffer 0: waiting for read
            // buffer 1: old
            // buffer 2: old

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(0));

                // buffer 0: reading
                // buffer 1: old
                // buffer 2: old

                using (var write = tripleBuffer.GetForWrite())
                {
                    Assert.That(write.Object?.ID, Is.EqualTo(1));

                    // buffer 0: reading
                    // buffer 1: writing
                    // buffer 2: old
                }
            }

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(1));

                // buffer 0: old
                // buffer 1: reading
                // buffer 2: old
            }

            using (var write = tripleBuffer.GetForWrite())
            {
                Assert.That(write.Object?.ID, Is.EqualTo(0));

                // buffer 0: writing
                // buffer 1: last read
                // buffer 2: old
            }

            using (var read = tripleBuffer.GetForRead())
            {
                Assert.That(read?.Object?.ID, Is.EqualTo(0));

                // buffer 0: reading
                // buffer 1: old
                // buffer 2: old

                using (var write = tripleBuffer.GetForWrite())
                {
                    Assert.That(write.Object?.ID, Is.EqualTo(1));

                    // buffer 0: reading
                    // buffer 1: writing
                    // buffer 2: old
                }

                using (var write = tripleBuffer.GetForWrite())
                {
                    Assert.That(write.Object?.ID, Is.EqualTo(2));

                    // buffer 0: reading
                    // buffer 1: waiting for read
                    // buffer 2: writing
                }
            }
        }

        [Test]
        public void TestWriteThenRead()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 1000; i++)
            {
                var obj = new TestObject(i);

                using (var write = tripleBuffer.GetForWrite())
                    write.Object = obj;

                using (var buffer = tripleBuffer.GetForRead())
                    Assert.That(buffer?.Object, Is.EqualTo(obj));
            }

            using (var buffer = tripleBuffer.GetForRead())
                Assert.That(buffer, Is.Null);
        }

        [Test]
        public void TestReadSaturated()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            for (int i = 0; i < 10; i++)
            {
                var obj = new TestObject(i);
                ManualResetEventSlim resetEventSlim = new ManualResetEventSlim();

                var readTask = Task.Factory.StartNew(() =>
                {
                    resetEventSlim.Set();
                    using (var buffer = tripleBuffer.GetForRead())
                        Assert.That(buffer?.Object, Is.EqualTo(obj));
                }, TaskCreationOptions.LongRunning);

                Task.Factory.StartNew(() =>
                {
                    resetEventSlim.Wait(1000);
                    Thread.Sleep(10);

                    using (var write = tripleBuffer.GetForWrite())
                        write.Object = obj;
                }, TaskCreationOptions.LongRunning);

                readTask.WaitSafely();
            }
        }

        private static TripleBuffer<TestObject> createWithIDsMatchingIndices()
        {
            var tripleBuffer = new TripleBuffer<TestObject>();

            // Setup the triple buffer with correctly indexed objects.
            List<int> initialisedBuffers = new List<int>();

            initialiseBuffer();

            using (var _ = tripleBuffer.GetForRead())
            {
                initialiseBuffer();
                initialiseBuffer();
            }

            Assert.That(initialisedBuffers, Is.EqualTo(new[] { 0, 1, 2 }));

            // Read remaining buffers to reset things to a sane state (next write will be at index 0).
            using (var _ = tripleBuffer.GetForRead()) { }

            using (var _ = tripleBuffer.GetForRead()) { }

            void initialiseBuffer()
            {
                using (var write = tripleBuffer.GetForWrite())
                {
                    write.Object = new TestObject(write.Index);
                    initialisedBuffers.Add(write.Index);
                }
            }

            return tripleBuffer;
        }

        private class TestObject
        {
            public readonly int ID;

            public TestObject(int id)
            {
                ID = id;
            }

            public override string ToString()
            {
                return $"{base.ToString()} ID: {ID}";
            }
        }
    }
}
