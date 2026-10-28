// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkEventList
    {
        // Used for benchmark-local testing.
        private ResourceAllocator localAllocator = null!;
        private List<RenderEvent> localEventList = null!;

        // Used for benchmark-static testing.
        // 0: Basic events
        // 1: Events with data
        // 2: Mixed events
        private readonly (ResourceAllocator allocator, List<RenderEvent> list)[] staticItems = new (ResourceAllocator allocator, List<RenderEvent> list)[3];

        [GlobalSetup]
        public void GlobalSetup()
        {
            localAllocator = new ResourceAllocator();
            localEventList = new List<RenderEvent>();

            for (int i = 0; i < staticItems.Length; i++)
            {
                ResourceAllocator allocator = new ResourceAllocator();
                staticItems[i] = (allocator, new List<RenderEvent>());
            }

            for (int i = 0; i < 10000; i++)
            {
                staticItems[0].list.Add(RenderEvent.Create(new FlushEvent(new ResourceReference(1), 10)));
                staticItems[1].list.Add(RenderEvent.Create(new AddPrimitiveToBatchEvent(new ResourceReference(0), staticItems[1].allocator.AllocateRegion(1024))));
                staticItems[2].list.Add(i % 2 == 0
                    ? RenderEvent.Create(new FlushEvent(new ResourceReference(1), 10))
                    : RenderEvent.Create(new AddPrimitiveToBatchEvent(new ResourceReference(0), staticItems[2].allocator.AllocateRegion(1024))));
            }
        }

        [Benchmark]
        public void Write()
        {
            localAllocator.NewFrame();

            for (int i = 0; i < 10000; i++)
                localEventList.Add(RenderEvent.Create(new FlushEvent()));
        }

        [Benchmark]
        public void WriteWithData()
        {
            localAllocator.NewFrame();

            for (int i = 0; i < 10000; i++)
                localEventList.Add(RenderEvent.Create(new AddPrimitiveToBatchEvent(new ResourceReference(0), localAllocator.AllocateRegion(1024))));
        }

        [Benchmark]
        public int Read()
        {
            int totalVertices = 0;

            foreach (var renderEvent in staticItems[0].list)
            {
                switch (renderEvent.Type)
                {
                    case RenderEventType.Flush:
                        FlushEvent e = (FlushEvent)renderEvent;
                        totalVertices += e.VertexCount;
                        break;
                }
            }

            return totalVertices;
        }

        [Benchmark]
        public int ReadWithData()
        {
            int data = 0;

            foreach (var renderEvent in staticItems[1].list)
            {
                switch (renderEvent.Type)
                {
                    case RenderEventType.AddPrimitiveToBatch:
                        AddPrimitiveToBatchEvent e = (AddPrimitiveToBatchEvent)renderEvent;
                        foreach (byte b in staticItems[1].allocator.GetRegion(e.Memory))
                            data += b;
                        break;
                }
            }

            return data;
        }

        [Benchmark]
        public int ReadMixed()
        {
            int data = 0;

            foreach (var renderEvent in staticItems[2].list)
            {
                switch (renderEvent.Type)
                {
                    case RenderEventType.Flush:
                    {
                        FlushEvent e = (FlushEvent)renderEvent;
                        data += e.VertexCount;
                        break;
                    }

                    case RenderEventType.AddPrimitiveToBatch:
                    {
                        AddPrimitiveToBatchEvent e = (AddPrimitiveToBatchEvent)renderEvent;
                        foreach (byte b in staticItems[2].allocator.GetRegion(e.Memory))
                            data += b;
                        break;
                    }
                }
            }

            return data;
        }
    }
}
