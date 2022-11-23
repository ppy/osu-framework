// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple rectangular box. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Box : Sprite
    {
        public Box()
        {
            // Setting the texture would normally set a size of (1, 1), but since the texture is set from BDL it needs to be set here instead.
            // RelativeSizeAxes may not behave as expected if this is not done.
            Size = Vector2.One;
        }

        [BackgroundDependencyLoader]
        private void load(IRenderer renderer)
        {
            base.Texture = renderer.WhitePixel;
        }

        public override Texture Texture
        {
            get => base.Texture;
            set => throw new InvalidOperationException($"The texture of a {nameof(Box)} cannot be set.");
        }
    }
}
