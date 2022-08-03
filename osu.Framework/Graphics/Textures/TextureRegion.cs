// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A sub-region of a <see cref="Texture"/>.
    /// </summary>
    public class TextureRegion : Texture
    {
        private readonly Texture parent;
        private readonly RectangleI bounds;

        /// <summary>
        /// Creates a new sub-region of a <see cref="Texture"/>.
        /// </summary>
        /// <param name="parent">The <see cref="Texture"/> to create a sub-region of.</param>
        /// <param name="bounds">The texture-space area in <paramref name="parent"/> which bounds this texture.</param>
        /// <param name="wrapModeS">The horizontal wrap mode for this region.</param>
        /// <param name="wrapModeT">The vertical warp mode for this region.</param>
        public TextureRegion(Texture parent, RectangleI bounds, WrapMode wrapModeS, WrapMode wrapModeT)
            : base(parent, wrapModeS, wrapModeT)
        {
            this.parent = parent;
            this.bounds = bounds;
        }

        public override int Width => bounds.Width;

        public override int Height => bounds.Height;

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? opacity)
        {
            if (upload.Bounds.Width > bounds.Width || upload.Bounds.Height > bounds.Height)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(upload),
                    $"Texture is too small to fit the requested upload. Texture size is {bounds.Width} x {bounds.Height}, upload size is {upload.Bounds.Width} x {upload.Bounds.Height}.");
            }

            if (upload.Bounds.IsEmpty)
                upload.Bounds = bounds;
            else
            {
                var adjustedBounds = upload.Bounds;

                adjustedBounds.X += bounds.X;
                adjustedBounds.Y += bounds.Y;

                upload.Bounds = adjustedBounds;
            }

            UpdateOpacity(upload, ref opacity);
            parent.SetData(upload, wrapModeS, wrapModeT, opacity);
        }

        private RectangleF boundsInParent(RectangleF? area)
        {
            // Bounds are already in texture space.
            RectangleF actualBounds = bounds;

            if (area is RectangleF rect)
            {
                // The incoming area is in display space, so it needs to be converted into texture space.
                rect *= ScaleAdjust;

                actualBounds.X += rect.X;
                actualBounds.Y += rect.Y;
                actualBounds.Width = rect.Width;
                actualBounds.Height = rect.Height;
            }

            // Convert the texture space area into the parent's display space.
            return actualBounds / parent.ScaleAdjust;
        }

        public override RectangleF GetTextureRect(RectangleF? area = null) => parent.GetTextureRect(boundsInParent(area));
    }
}
