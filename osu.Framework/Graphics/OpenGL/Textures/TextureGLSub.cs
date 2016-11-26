// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Drawing;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using OpenTK.Graphics;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using OpenTK;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    class TextureGLSub : TextureGL
    {
        private TextureGLSingle parent;
        private Rectangle bounds;

        public override TextureGL Native => parent.Native;

        public override int TextureId => parent.TextureId;
        public override bool Loaded => parent.Loaded;

        public TextureGLSub(Rectangle bounds, TextureGLSingle parent)
        {
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

        private RectangleF BoundsInParent(RectangleF? textureRect)
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
            return parent.GetTextureRect(BoundsInParent(textureRect));
        }

        /// <summary>
        /// Blits sprite to OpenGL display with specified parameters.
        /// </summary>
        public override void Draw(Quad vertexQuad, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            parent.Draw(vertexQuad, BoundsInParent(textureRect), drawColour, vertexAction, inflationPercentage);
        }

        internal override bool Upload()
        {
            //no upload required; our parent does this.
            return false;
        }

        public override bool Bind()
        {
            Debug.Assert(!isDisposed);

            Upload();

            return parent.Bind();
        }

        public override void SetData(TextureUpload upload)
        {
            Debug.Assert(upload.Bounds.Width <= bounds.Width && upload.Bounds.Height <= bounds.Height);

            if (upload.Bounds.Size.IsEmpty)
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
