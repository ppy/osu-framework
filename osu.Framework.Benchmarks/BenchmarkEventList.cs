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
                filledEventList.Enqueue(new FlushEvent(new ResourceReference(1), 10));
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
            var reader = filledEventList.CreateReader();

            int totalVertices = 0;

            while (reader.Next())
            {
                switch (reader.CurrentType())
                {
                    case RenderEventType.Flush:
                        ref FlushEvent e = ref reader.Current<FlushEvent>();
                        totalVertices += e.VertexCount;
                        break;
                }
            }

            return totalVertices;
        }
    }
}
