// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    [StructLayout(LayoutKind.Explicit)]
    internal record struct RenderEvent
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
        private SetUniformBufferEvent setUniformBuffer;

        [FieldOffset(1)]
        private SetViewportEvent setViewport;

        public RenderEvent(AddPrimitiveToBatchEvent @event)
        {
            Type = RenderEventType.AddPrimitiveToBatch;
            addPrimitiveToBatch = @event;
        }

        public RenderEvent(ClearEvent @event)
        {
            Type = RenderEventType.Clear;
            clear = @event;
        }

        public RenderEvent(DrawNodeActionEvent @event)
        {
            Type = RenderEventType.DrawNodeAction;
            drawNodeAction = @event;
        }

        public RenderEvent(FlushEvent @event)
        {
            Type = RenderEventType.Flush;
            flush = @event;
        }

        public RenderEvent(ResizeFrameBufferEvent @event)
        {
            Type = RenderEventType.ResizeFrameBuffer;
            resizeFrameBuffer = @event;
        }

        public RenderEvent(SetBlendEvent @event)
        {
            Type = RenderEventType.SetBlend;
            setBlend = @event;
        }

        public RenderEvent(SetBlendMaskEvent @event)
        {
            Type = RenderEventType.SetBlendMask;
            setBlendMask = @event;
        }

        public RenderEvent(SetDepthInfoEvent @event)
        {
            Type = RenderEventType.SetDepthInfo;
            setDepthInfo = @event;
        }

        public RenderEvent(SetFrameBufferEvent @event)
        {
            Type = RenderEventType.SetFrameBuffer;
            setFrameBuffer = @event;
        }

        public RenderEvent(SetScissorEvent @event)
        {
            Type = RenderEventType.SetScissor;
            setScissor = @event;
        }

        public RenderEvent(SetShaderEvent @event)
        {
            Type = RenderEventType.SetShader;
            setShader = @event;
        }

        public RenderEvent(SetScissorStateEvent @event)
        {
            Type = RenderEventType.SetScissorState;
            setScissorState = @event;
        }

        public RenderEvent(SetShaderStorageBufferObjectDataEvent @event)
        {
            Type = RenderEventType.SetShaderStorageBufferObjectData;
            setShaderStorageBufferObjectData = @event;
        }

        public RenderEvent(SetStencilInfoEvent @event)
        {
            Type = RenderEventType.SetStencilInfo;
            setStencilInfo = @event;
        }

        public RenderEvent(SetTextureEvent @event)
        {
            Type = RenderEventType.SetTexture;
            setTexture = @event;
        }

        public RenderEvent(SetUniformBufferDataEvent @event)
        {
            Type = RenderEventType.SetUniformBufferData;
            setUniformBufferData = @event;
        }

        public RenderEvent(SetUniformBufferEvent @event)
        {
            Type = RenderEventType.SetUniformBuffer;
            setUniformBuffer = @event;
        }

        public RenderEvent(SetViewportEvent @event)
        {
            Type = RenderEventType.SetViewport;
            setViewport = @event;
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
