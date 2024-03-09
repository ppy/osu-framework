// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Events
{
    internal enum RenderEventType : byte
    {
        AddPrimitiveToBatch,
        SetFrameBuffer,
        ResizeFrameBuffer,
        SetShader,
        SetTexture,
        SetUniformBuffer,
        Clear,
        SetDepthInfo,
        SetScissor,
        SetScissorState,
        SetStencilInfo,
        SetViewport,
        SetBlend,
        SetBlendMask,
        SetUniformBufferData,
        SetUniformBufferDataRange,
        SetShaderStorageBufferObjectData,
        Flush,

        DrawNodeAction,
    }
}
