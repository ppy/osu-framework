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

            int? lastWrite = null;

            for (int i = 0; i < 3; i++)
            {
                using (var write = tripleBuffer.GetForWrite())
                {
                    Assert.That(write.Object!.ID, Is.Not.EqualTo(lastWrite));
                    lastWrite = write.Object!.ID;
                }

                using (var buffer = tripleBuffer.GetForRead())
                    Assert.That(buffer!.Object!.ID, Is.EqualTo(lastWrite));
            }
        }

        [Test]
        public void TestSameBufferIsNotWrittenTwiceInRowContestation()
        {
            var tripleBuffer = createWithIDsMatchingIndices();

            // Test with first write in use during second.
            using (tripleBuffer.GetForWrite())
            {
            }

            int? lastRead = null;
            int? lastWrite = null;

            for (int i = 0; i < 3; i++)
            {
                using (var read = tripleBuffer.GetForRead())
                {
                    Assert.That(read!.Object!.ID, Is.Not.EqualTo(lastRead));

                    for (int j = 0; j < 3; j++)
                    {
                        using (var write = tripleBuffer.GetForWrite())
                        {
                            Assert.That(write.Object!.ID, Is.Not.EqualTo(lastWrite));
                            Assert.That(write.Object!.ID, Is.Not.EqualTo(read.Object?.ID));
                            lastWrite = write.Object!.ID;
                        }
                    }
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
