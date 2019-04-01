// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics
{
    public interface ITexturedDrawable : ITexturedShaderDrawable
    {
        Texture Texture { get; }

        RectangleF DrawRectangle { get; }

        Vector2 InflationAmount { get; }

        bool WrapTexture { get; }
    }
}
