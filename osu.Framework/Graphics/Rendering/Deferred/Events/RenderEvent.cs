// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    [SkipLocalsInit]
    [StructLayout(LayoutKind.Explicit)]
    internal struct RenderEvent
    {
        /// <summary>
        /// The type of render event.
        /// </summary>
        [FieldOffset(0)]
        public RenderEventType Type;

        [FieldOffset(1)]
        private AddPrimitiveToBatchEvent addPrimitiveToBatch;

        [FieldOffset(1)]
        private ClearEvent clear;

        [FieldOffset(1)]
        private DrawNodeActionEvent drawNodeAction;

        [FieldOffset(1)]
        private FlushEvent flush;

        [FieldOffset(1)]
        private ResizeFrameBufferEvent resizeFrameBuffer;

        [FieldOffset(1)]
        private SetBlendEvent setBlend;

        [FieldOffset(1)]
        private SetBlendMaskEvent setBlendMask;

        [FieldOffset(1)]
        private SetDepthInfoEvent setDepthInfo;

        [FieldOffset(1)]
        private SetFrameBufferEvent setFrameBuffer;

        [FieldOffset(1)]
        private SetScissorEvent setScissor;

        [FieldOffset(1)]
        private SetShaderEvent setShader;

        [FieldOffset(1)]
        private SetScissorStateEvent setScissorState;

        [FieldOffset(1)]
        private SetShaderStorageBufferObjectDataEvent setShaderStorageBufferObjectData;

        [FieldOffset(1)]
        private SetStencilInfoEvent setStencilInfo;

        [FieldOffset(1)]
        private SetTextureEvent setTexture;

        [FieldOffset(1)]
        private SetUniformBufferDataEvent setUniformBufferData;

        [FieldOffset(1)]
        private SetUniformBufferDataRangeEvent setUniformBufferDataRange;

        [FieldOffset(1)]
        private SetUniformBufferEvent setUniformBuffer;

        [FieldOffset(1)]
        private SetViewportEvent setViewport;

        public static RenderEvent Init(in AddPrimitiveToBatchEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.AddPrimitiveToBatch;
            e.addPrimitiveToBatch = @event;
            return e;
        }

        public static RenderEvent Init(in ClearEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.Clear;
            e.clear = @event;
            return e;
        }

        public static RenderEvent Init(in DrawNodeActionEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.DrawNodeAction;
            e.drawNodeAction = @event;
            return e;
        }

        public static RenderEvent Init(in FlushEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.Flush;
            e.flush = @event;
            return e;
        }

        public static RenderEvent Init(in ResizeFrameBufferEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.ResizeFrameBuffer;
            e.resizeFrameBuffer = @event;
            return e;
        }

        public static RenderEvent Init(in SetBlendEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetBlend;
            e.setBlend = @event;
            return e;
        }

        public static RenderEvent Init(in SetBlendMaskEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetBlendMask;
            e.setBlendMask = @event;
            return e;
        }

        public static RenderEvent Init(in SetDepthInfoEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetDepthInfo;
            e.setDepthInfo = @event;
            return e;
        }

        public static RenderEvent Init(in SetFrameBufferEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetFrameBuffer;
            e.setFrameBuffer = @event;
            return e;
        }

        public static RenderEvent Init(in SetScissorEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetScissor;
            e.setScissor = @event;
            return e;
        }

        public static RenderEvent Init(in SetShaderEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetShader;
            e.setShader = @event;
            return e;
        }

        public static RenderEvent Init(in SetScissorStateEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetScissorState;
            e.setScissorState = @event;
            return e;
        }

        public static RenderEvent Init(in SetShaderStorageBufferObjectDataEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetShaderStorageBufferObjectData;
            e.setShaderStorageBufferObjectData = @event;
            return e;
        }

        public static RenderEvent Init(in SetStencilInfoEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetStencilInfo;
            e.setStencilInfo = @event;
            return e;
        }

        public static RenderEvent Init(in SetTextureEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetTexture;
            e.setTexture = @event;
            return e;
        }

        public static RenderEvent Init(in SetUniformBufferDataEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetUniformBufferData;
            e.setUniformBufferData = @event;
            return e;
        }

        public static RenderEvent Init(in SetUniformBufferDataRangeEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetUniformBufferDataRange;
            e.setUniformBufferDataRange = @event;
            return e;
        }

        public static RenderEvent Init(in SetUniformBufferEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetUniformBuffer;
            e.setUniformBuffer = @event;
            return e;
        }

        public static RenderEvent Init(in SetViewportEvent @event)
        {
            Unsafe.SkipInit(out RenderEvent e);
            e.Type = RenderEventType.SetViewport;
            e.setViewport = @event;
            return e;
        }

        public void Decompose(out AddPrimitiveToBatchEvent @event)
        {
            @event = addPrimitiveToBatch;
        }

        public void Decompose(out ClearEvent @event)
        {
            @event = clear;
        }

        public void Decompose(out DrawNodeActionEvent @event)
        {
            @event = drawNodeAction;
        }

        public void Decompose(out FlushEvent @event)
        {
            @event = flush;
        }

        public void Decompose(out ResizeFrameBufferEvent @event)
        {
            @event = resizeFrameBuffer;
        }

        public void Decompose(out SetBlendEvent @event)
        {
            @event = setBlend;
        }

        public void Decompose(out SetBlendMaskEvent @event)
        {
            @event = setBlendMask;
        }

        public void Decompose(out SetDepthInfoEvent @event)
        {
            @event = setDepthInfo;
        }

        public void Decompose(out SetFrameBufferEvent @event)
        {
            @event = setFrameBuffer;
        }

        public void Decompose(out SetScissorEvent @event)
        {
            @event = setScissor;
        }

        public void Decompose(out SetShaderEvent @event)
        {
            @event = setShader;
        }

        public void Decompose(out SetScissorStateEvent @event)
        {
            @event = setScissorState;
        }

        public void Decompose(out SetShaderStorageBufferObjectDataEvent @event)
        {
            @event = setShaderStorageBufferObjectData;
        }

        public void Decompose(out SetStencilInfoEvent @event)
        {
            @event = setStencilInfo;
        }

        public void Decompose(out SetTextureEvent @event)
        {
            @event = setTexture;
        }

        public void Decompose(out SetUniformBufferDataEvent @event)
        {
            @event = setUniformBufferData;
        }

        public void Decompose(out SetUniformBufferDataRangeEvent @event)
        {
            @event = setUniformBufferDataRange;
        }

        public void Decompose(out SetUniformBufferEvent @event)
        {
            @event = setUniformBuffer;
        }

        public void Decompose(out SetViewportEvent @event)
        {
            @event = setViewport;
        }
    }
}
