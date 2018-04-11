// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;

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
            get { return bounds.Height; }
            set { bounds.Height = value; }
        }

        public override int Width
        {
            get { return bounds.Width; }
            set { bounds.Width = value; }
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

        public override RectangleF GetTextureRect(RectangleF? textureRect)
        {
            return parent.GetTextureRect(boundsInParent(textureRect));
        }

        public override void DrawTriangle(Triangle vertexTriangle, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            parent.DrawTriangle(vertexTriangle, boundsInParent(textureRect), drawColour, vertexAction, inflationPercentage);
        }

        public override void DrawQuad(Quad vertexQuad, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
        {
            parent.DrawQuad(vertexQuad, boundsInParent(textureRect), drawColour, vertexAction, inflationPercentage, blendRangeOverride);
        }

        internal override bool Upload()
        {
            //no upload required; our parent does this.
            return false;
        }

        public override bool Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed sub textures.");

            Upload();

            return parent.Bind();
        }

        public override void SetData(TextureUpload upload)
        {
            if (upload.Bounds.Width > bounds.Width || upload.Bounds.Height > bounds.Height)
                throw new ArgumentOutOfRangeException(
                    $"Texture is too small to fit the requested upload. Texture size is {bounds.Width} x {bounds.Height}, upload size is {upload.Bounds.Width} x {upload.Bounds.Height}.",
                    nameof(upload));

            if (upload.Bounds.IsEmpty)
                upload.Bounds = bounds;
            else
            {
                upload.Bounds.X += bounds.X;
                upload.Bounds.Y += bounds.Y;
            }

            parent?.SetData(upload);
        }
    }
}
