// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
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

        public static RenderEvent Create(in AddPrimitiveToBatchEvent @event) => new RenderEvent
        {
            Type = RenderEventType.AddPrimitiveToBatch,
            addPrimitiveToBatch = @event
        };

        public static RenderEvent Create(in ClearEvent @event) => new RenderEvent
        {
            Type = RenderEventType.Clear,
            clear = @event
        };

        public static RenderEvent Create(in DrawNodeActionEvent @event) => new RenderEvent
        {
            Type = RenderEventType.DrawNodeAction,
            drawNodeAction = @event
        };

        public static RenderEvent Create(in FlushEvent @event) => new RenderEvent
        {
            Type = RenderEventType.Flush,
            flush = @event
        };

        public static RenderEvent Create(in ResizeFrameBufferEvent @event) => new RenderEvent
        {
            Type = RenderEventType.ResizeFrameBuffer,
            resizeFrameBuffer = @event
        };

        public static RenderEvent Create(in SetBlendEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetBlend,
            setBlend = @event
        };

        public static RenderEvent Create(in SetBlendMaskEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetBlendMask,
            setBlendMask = @event
        };

        public static RenderEvent Create(in SetDepthInfoEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetDepthInfo,
            setDepthInfo = @event
        };

        public static RenderEvent Create(in SetFrameBufferEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetFrameBuffer,
            setFrameBuffer = @event
        };

        public static RenderEvent Create(in SetScissorEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetScissor,
            setScissor = @event
        };

        public static RenderEvent Create(in SetShaderEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetShader,
            setShader = @event
        };

        public static RenderEvent Create(in SetScissorStateEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetScissorState,
            setScissorState = @event
        };

        public static RenderEvent Create(in SetShaderStorageBufferObjectDataEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetShaderStorageBufferObjectData,
            setShaderStorageBufferObjectData = @event
        };

        public static RenderEvent Create(in SetStencilInfoEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetStencilInfo,
            setStencilInfo = @event
        };

        public static RenderEvent Create(in SetTextureEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetTexture,
            setTexture = @event
        };

        public static RenderEvent Create(in SetUniformBufferDataEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetUniformBufferData,
            setUniformBufferData = @event
        };

        public static RenderEvent Create(in SetUniformBufferDataRangeEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetUniformBufferDataRange,
            setUniformBufferDataRange = @event
        };

        public static RenderEvent Create(in SetUniformBufferEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetUniformBuffer,
            setUniformBuffer = @event
        };

        public static RenderEvent Create(in SetViewportEvent @event) => new RenderEvent
        {
            Type = RenderEventType.SetViewport,
            setViewport = @event
        };

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
