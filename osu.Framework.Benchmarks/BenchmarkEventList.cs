// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Rendering.Deferred;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkEventList
    {
        private EventList filledEventList = null!;

        [GlobalSetup]
        public void GlobalSetup()
        {
            filledEventList = new EventList(new ResourceAllocator());

            for (int i = 0; i < 10000; i++)
                filledEventList.Enqueue(new FlushEvent(RenderEventType.Flush, new ResourceReference(1), 10));
        }

        [Benchmark]
        public void Write()
        {
            ResourceAllocator allocator = new ResourceAllocator();
            EventList list = new EventList(allocator);

            for (int i = 0; i < 10000; i++)
                list.Enqueue(new FlushEvent());

            allocator.NewFrame();
        }

        [Benchmark]
        public int Read()
        {
            var enumerator = filledEventList.CreateEnumerator();

            int totalVertices = 0;

            while (enumerator.Next())
            {
                switch (enumerator.CurrentType())
                {
                    case RenderEventType.Flush:
                        ref FlushEvent e = ref enumerator.Current<FlushEvent>();
                        totalVertices += e.VertexCount;
                        break;
                }
            }

            return totalVertices;
        }

        [Benchmark]
        public int ReplaceSame()
        {
            ResourceAllocator allocator = new ResourceAllocator();
            EventList list = new EventList(allocator);

            list.Enqueue(new FlushEvent());
            list.Enqueue(new FlushEvent());
            list.Enqueue(new FlushEvent());

            var enumerator = filledEventList.CreateEnumerator();
            enumerator.Next();
            enumerator.Next();
            enumerator.Replace(new FlushEvent());

            int i = 0;
            enumerator = filledEventList.CreateEnumerator();

            while (enumerator.Next())
            {
                enumerator.CurrentType();
                i++;
            }

            allocator.NewFrame();

            return i;
        }

        [Benchmark]
        public int ReplaceSmaller()
        {
            ResourceAllocator allocator = new ResourceAllocator();
            EventList list = new EventList(allocator);

            list.Enqueue(new FlushEvent());
            list.Enqueue(new FlushEvent());
            list.Enqueue(new FlushEvent());

            var enumerator = filledEventList.CreateEnumerator();
            enumerator.Next();
            enumerator.Next();
            enumerator.Replace(new SetScissorStateEvent());

            int i = 0;
            enumerator = filledEventList.CreateEnumerator();

            while (enumerator.Next())
            {
                enumerator.CurrentType();
                i++;
            }

            allocator.NewFrame();

            return i;
        }

        [Benchmark]
        public int ReplaceBigger()
        {
            ResourceAllocator allocator = new ResourceAllocator();
            EventList list = new EventList(allocator);

            list.Enqueue(new FlushEvent());
            list.Enqueue(new FlushEvent());
            list.Enqueue(new FlushEvent());

            var enumerator = filledEventList.CreateEnumerator();
            enumerator.Next();
            enumerator.Next();
            enumerator.Replace(new SetUniformBufferDataEvent());

            int i = 0;
            enumerator = filledEventList.CreateEnumerator();

            while (enumerator.Next())
            {
                enumerator.CurrentType();
                i++;
            }

            allocator.NewFrame();

            return i;
        }
    }
}
