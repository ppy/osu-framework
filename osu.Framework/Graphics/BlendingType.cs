// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics
{
    public enum BlendingType
    {
        Inherit = 0,
        ConstantAlpha,
        ConstantColor,
        DstAlpha,
        DstColor,
        One,
        OneMinusConstantAlpha,
        OneMinusConstantColor,
        OneMinusDstAlpha,
        OneMinusDstColor,
        OneMinusSrcAlpha,
        OneMinusSrcColor,
        SrcAlpha,
        SrcAlphaSaturate,
        SrcColor,
        Zero
    }
}
