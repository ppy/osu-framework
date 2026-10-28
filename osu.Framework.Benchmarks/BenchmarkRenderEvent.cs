// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using osu.Framework.Graphics.Rendering.Deferred.Events;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkRenderEvent
    {
        private readonly Consumer consumer = new Consumer();

        [Benchmark]
        public void AddPrimitiveToBatch()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(AddPrimitiveToBatchEvent)));
        }

        [Benchmark]
        public void Clear()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(ClearEvent)));
        }

        [Benchmark]
        public void DrawNodeAction()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(DrawNodeActionEvent)));
        }

        [Benchmark]
        public void Flush()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(FlushEvent)));
        }

        [Benchmark]
        public void ResizeFrameBuffer()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(ResizeFrameBufferEvent)));
        }

        [Benchmark]
        public void SetBlend()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetBlendEvent)));
        }

        [Benchmark]
        public void SetBlendMask()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetBlendMaskEvent)));
        }

        [Benchmark]
        public void SetDepthInfo()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetDepthInfoEvent)));
        }

        [Benchmark]
        public void SetFrameBuffer()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetFrameBufferEvent)));
        }

        [Benchmark]
        public void SetScissor()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetScissorEvent)));
        }

        [Benchmark]
        public void SetShader()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetShaderEvent)));
        }

        [Benchmark]
        public void SetScissorState()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetScissorStateEvent)));
        }

        [Benchmark]
        public void SetShaderStorageBufferObjectData()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetShaderStorageBufferObjectDataEvent)));
        }

        [Benchmark]
        public void SetStencilInfo()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetStencilInfoEvent)));
        }

        [Benchmark]
        public void SetTexture()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetTextureEvent)));
        }

        [Benchmark]
        public void SetUniformBufferData()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetUniformBufferDataEvent)));
        }

        [Benchmark]
        public void SetUniformBufferDataRange()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetUniformBufferDataRangeEvent)));
        }

        [Benchmark]
        public void SetUniformBuffer()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetUniformBufferEvent)));
        }

        [Benchmark]
        public void SetViewport()
        {
            for (int i = 0; i < 10000; i++)
                consumer.Consume(RenderEvent.Create(default(SetViewportEvent)));
        }
    }
}
