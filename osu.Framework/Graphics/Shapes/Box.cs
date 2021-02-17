// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple rectangular box. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Box : Sprite
    {
        public Box()
        {
            base.Texture = Texture.WhitePixel;
        }

        public override Texture Texture
        {
            get => base.Texture;
            set => throw new InvalidOperationException($"The texture of a {nameof(Box)} cannot be set.");
        }
    }
}
