// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    public enum FlushBatchSource
    {
        BindTexture,
        FinishFrame,
        SetBlend,
        SetBlendMask,
        SetDepthInfo,
        SetFrameBuffer,
        SetMasking,
        SetProjection,
        SetScissor,
        SetShader,
        SetStencilInfo,
        SetUniform,
        UnbindTexture,
        SetActiveBatch
    }
}
