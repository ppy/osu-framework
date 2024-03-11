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

        public static explicit operator AddPrimitiveToBatchEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.AddPrimitiveToBatch);
            return @event.addPrimitiveToBatch;
        }

        public static explicit operator ClearEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.Clear);
            return @event.clear;
        }

        public static explicit operator DrawNodeActionEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.DrawNodeAction);
            return @event.drawNodeAction;
        }

        public static explicit operator FlushEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.Flush);
            return @event.flush;
        }

        public static explicit operator ResizeFrameBufferEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.ResizeFrameBuffer);
            return @event.resizeFrameBuffer;
        }

        public static explicit operator SetBlendEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetBlend);
            return @event.setBlend;
        }

        public static explicit operator SetBlendMaskEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetBlendMask);
            return @event.setBlendMask;
        }

        public static explicit operator SetDepthInfoEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetDepthInfo);
            return @event.setDepthInfo;
        }

        public static explicit operator SetFrameBufferEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetFrameBuffer);
            return @event.setFrameBuffer;
        }

        public static explicit operator SetScissorEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetScissor);
            return @event.setScissor;
        }

        public static explicit operator SetShaderEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetShader);
            return @event.setShader;
        }

        public static explicit operator SetScissorStateEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetScissorState);
            return @event.setScissorState;
        }

        public static explicit operator SetShaderStorageBufferObjectDataEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetShaderStorageBufferObjectData);
            return @event.setShaderStorageBufferObjectData;
        }

        public static explicit operator SetStencilInfoEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetStencilInfo);
            return @event.setStencilInfo;
        }

        public static explicit operator SetTextureEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetTexture);
            return @event.setTexture;
        }

        public static explicit operator SetUniformBufferDataEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetUniformBufferData);
            return @event.setUniformBufferData;
        }

        public static explicit operator SetUniformBufferDataRangeEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetUniformBufferDataRange);
            return @event.setUniformBufferDataRange;
        }

        public static explicit operator SetUniformBufferEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetUniformBuffer);
            return @event.setUniformBuffer;
        }

        public static explicit operator SetViewportEvent(RenderEvent @event)
        {
            Debug.Assert(@event.Type == RenderEventType.SetViewport);
            return @event.setViewport;
        }
    }
}
