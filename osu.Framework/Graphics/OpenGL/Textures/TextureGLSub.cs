// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osuTK;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal class TextureGLSub : TextureGL
    {
        private readonly TextureGLSingle parent;
        private RectangleI bounds;

        public override TextureGL Native => parent.Native;

        public override int TextureId => parent.TextureId;
        public override bool Loaded => parent.Loaded;

        public TextureGLSub(RectangleI bounds, TextureGLSingle parent)
        {
            // If GLWrapper is not initialized at this point, it means we do not have OpenGL available
            // and thus will never draw anything. In this case it is fine if the parent texture is null.
            if (GLWrapper.IsInitialized && parent == null)
                throw new InvalidOperationException("May not construct a subtexture without a parent texture to refer to.");

            this.bounds = bounds;
            this.parent = parent;
        }

        public override int Height
        {
            get => bounds.Height;
            set => bounds.Height = value;
        }

        public override int Width
        {
            get => bounds.Width;
            set => bounds.Width = value;
        }

        private RectangleF boundsInParent(RectangleF? textureRect)
        {
            RectangleF actualBounds = bounds;

            if (textureRect.HasValue)
            {
                RectangleF localBounds = textureRect.Value;
                actualBounds.X += localBounds.X;
                actualBounds.Y += localBounds.Y;
                actualBounds.Width = Math.Min(localBounds.Width, bounds.Width);
                actualBounds.Height = Math.Min(localBounds.Height, bounds.Height);
            }

            return actualBounds;
        }

        public override RectangleF GetTextureRect(RectangleF? textureRect) => parent.GetTextureRect(boundsInParent(textureRect));

        internal override void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                            Vector2? inflationPercentage = null)
        {
            parent.DrawTriangle(vertexTriangle, drawColour, boundsInParent(textureRect), vertexAction, inflationPercentage);
        }

        internal override void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null)
        {
            parent.DrawQuad(vertexQuad, drawColour, boundsInParent(textureRect), vertexAction, inflationPercentage: inflationPercentage, blendRangeOverride: blendRangeOverride);
        }

        internal override bool Upload() => false;

        internal override void FlushUploads()
        {
        }

        public override bool Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed sub textures.");

            Upload();

            return parent.Bind(unit);
        }

        public override void SetData(ITextureUpload upload)
        {
            if (upload.Bounds.Width > bounds.Width || upload.Bounds.Height > bounds.Height)
                throw new ArgumentOutOfRangeException(
                    $"Texture is too small to fit the requested upload. Texture size is {bounds.Width} x {bounds.Height}, upload size is {upload.Bounds.Width} x {upload.Bounds.Height}.",
                    nameof(upload));

            if (upload.Bounds.IsEmpty)
                upload.Bounds = bounds;
            else
            {
                var adjustedBounds = upload.Bounds;

                adjustedBounds.X += bounds.X;
                adjustedBounds.Y += bounds.Y;

                upload.Bounds = adjustedBounds;
            }

            parent?.SetData(upload);
        }
    }
}
