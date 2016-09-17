//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Graphics.ES20;
using PixelFormat = OpenTK.Graphics.ES20.PixelFormat;
using System.Diagnostics;
using System.Drawing;
using OpenTK.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Batches;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    class TextureGLSub : TextureGL
    {
        private TextureGLSingle parent;
        private Rectangle bounds;

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

        /// <summary>
        /// Blits sprite to OpenGL display with specified parameters.
        /// </summary>
        public override void Draw(Quad vertexQuad, RectangleF? textureRect, Color4 drawColour, VertexBatch<TexturedVertex2d> spriteBatch = null)
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

            parent.Draw(vertexQuad, actualBounds, drawColour, spriteBatch);
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

            upload.Bounds = bounds;

            parent.SetData(upload);
        }
    }
}
