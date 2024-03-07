// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Rendering.Deferred;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class EventListTest
    {
        private ResourceAllocator allocator = null!;
        private EventList list = null!;

        [SetUp]
        public void Setup()
        {
            allocator = new ResourceAllocator();
            list = new EventList(allocator);
        }

        [TearDown]
        public void TearDown()
        {
            allocator.NewFrame();
        }

        [Test]
        public void ReplaceWithSmallEvent()
        {
            list.Enqueue(new FlushEvent());

            var enumerator = list.CreateEnumerator();
            enumerator.Next();
            enumerator.Replace(new FlushEvent());
        }

        [Test]
        public void ReplaceWithLargeEvent()
        {
            list.Enqueue(new FlushEvent());

            var enumerator = list.CreateEnumerator();
            enumerator.Next();
            enumerator.Replace(new SetUniformBufferDataEvent());
        }
    }
}
