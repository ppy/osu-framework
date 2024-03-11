// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
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

        [FieldOffset(8)]
        private AddPrimitiveToBatchEvent addPrimitiveToBatch;

        [FieldOffset(8)]
        private ClearEvent clear;

        [FieldOffset(8)]
        private DrawNodeActionEvent drawNodeAction;

        [FieldOffset(8)]
        private FlushEvent flush;

        [FieldOffset(8)]
        private ResizeFrameBufferEvent resizeFrameBuffer;

        [FieldOffset(8)]
        private SetBlendEvent setBlend;

        [FieldOffset(8)]
        private SetBlendMaskEvent setBlendMask;

        [FieldOffset(8)]
        private SetDepthInfoEvent setDepthInfo;

        [FieldOffset(8)]
        private SetFrameBufferEvent setFrameBuffer;

        [FieldOffset(8)]
        private SetScissorEvent setScissor;

        [FieldOffset(8)]
        private SetShaderEvent setShader;

        [FieldOffset(8)]
        private SetScissorStateEvent setScissorState;

        [FieldOffset(8)]
        private SetShaderStorageBufferObjectDataEvent setShaderStorageBufferObjectData;

        [FieldOffset(8)]
        private SetStencilInfoEvent setStencilInfo;

        [FieldOffset(8)]
        private SetTextureEvent setTexture;

        [FieldOffset(8)]
        private SetUniformBufferDataEvent setUniformBufferData;

        [FieldOffset(8)]
        private SetUniformBufferDataRangeEvent setUniformBufferDataRange;

        [FieldOffset(8)]
        private SetUniformBufferEvent setUniformBuffer;

        [FieldOffset(8)]
        private SetViewportEvent setViewport;

        [Obsolete("RenderEvent must be initialised via Init().", true)]
        public RenderEvent()
        {
        }

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
            Debug.Assert(Type == RenderEventType.AddPrimitiveToBatch);
            @event = addPrimitiveToBatch;
        }

        public void Decompose(out ClearEvent @event)
        {
            Debug.Assert(Type == RenderEventType.Clear);
            @event = clear;
        }

        public void Decompose(out DrawNodeActionEvent @event)
        {
            Debug.Assert(Type == RenderEventType.DrawNodeAction);
            @event = drawNodeAction;
        }

        public void Decompose(out FlushEvent @event)
        {
            Debug.Assert(Type == RenderEventType.Flush);
            @event = flush;
        }

        public void Decompose(out ResizeFrameBufferEvent @event)
        {
            Debug.Assert(Type == RenderEventType.ResizeFrameBuffer);
            @event = resizeFrameBuffer;
        }

        public void Decompose(out SetBlendEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetBlend);
            @event = setBlend;
        }

        public void Decompose(out SetBlendMaskEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetBlendMask);
            @event = setBlendMask;
        }

        public void Decompose(out SetDepthInfoEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetDepthInfo);
            @event = setDepthInfo;
        }

        public void Decompose(out SetFrameBufferEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetFrameBuffer);
            @event = setFrameBuffer;
        }

        public void Decompose(out SetScissorEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetScissor);
            @event = setScissor;
        }

        public void Decompose(out SetShaderEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetShader);
            @event = setShader;
        }

        public void Decompose(out SetScissorStateEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetScissorState);
            @event = setScissorState;
        }

        public void Decompose(out SetShaderStorageBufferObjectDataEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetShaderStorageBufferObjectData);
            @event = setShaderStorageBufferObjectData;
        }

        public void Decompose(out SetStencilInfoEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetStencilInfo);
            @event = setStencilInfo;
        }

        public void Decompose(out SetTextureEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetTexture);
            @event = setTexture;
        }

        public void Decompose(out SetUniformBufferDataEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetUniformBufferData);
            @event = setUniformBufferData;
        }

        public void Decompose(out SetUniformBufferDataRangeEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetUniformBufferDataRange);
            @event = setUniformBufferDataRange;
        }

        public void Decompose(out SetUniformBufferEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetUniformBuffer);
            @event = setUniformBuffer;
        }

        public void Decompose(out SetViewportEvent @event)
        {
            Debug.Assert(Type == RenderEventType.SetViewport);
            @event = setViewport;
        }
    }
}
