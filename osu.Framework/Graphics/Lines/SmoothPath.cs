// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Lines
{
    public class SmoothPath : Path
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            validateTexture();
        }

        public override float PathWidth
        {
            get => base.PathWidth;
            set
            {
                if (base.PathWidth == value)
                    return;
                base.PathWidth = value;

                InvalidateTexture();
            }
        }

        private Cached textureCache = new Cached();

        protected void InvalidateTexture()
        {
            textureCache.Invalidate();
            Invalidate(Invalidation.DrawNode);
        }

        private void validateTexture()
        {
            if (textureCache.IsValid)
                return;

            int textureWidth = (int)PathWidth * 2;

            var texture = new Texture(textureWidth, 1);

            //initialise background
            var raw = new Image<Rgba32>(textureWidth, 1);

            const float aa_portion = 0.02f;

            for (int i = 0; i < textureWidth; i++)
            {
                float progress = (float)i / (textureWidth - 1);

                var colour = ColourAt(progress);
                raw[i, 0] = new Rgba32(colour.R, colour.G, colour.B, colour.A * Math.Min(progress / aa_portion, 1));
            }

            texture.SetData(new TextureUpload(raw));
            Texture = texture;

            textureCache.Validate();
        }

        /// <summary>
        /// Retrieves the colour from a position in the texture of the <see cref="Path"/>.
        /// </summary>
        /// <param name="position">The position within the texture. 0 indicates the outermost-point of the path, 1 indicates the centre of the path.</param>
        /// <returns></returns>
        protected virtual Color4 ColourAt(float position) => Color4.White;

        protected override void ApplyDrawNode(DrawNode node)
        {
            validateTexture();

            base.ApplyDrawNode(node);
        }
    }
}
