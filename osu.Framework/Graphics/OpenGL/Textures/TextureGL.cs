// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Primitives;
using OpenTK.Graphics.ES30;
using OpenTK;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    public abstract class TextureGL : IDisposable
    {
        public bool IsTransparent;
        public TextureWrapMode WrapMode = TextureWrapMode.ClampToEdge;

        #region Disposal

        ~TextureGL()
        {
            Dispose(false);
        }

        protected bool IsDisposed;

        protected virtual void Dispose(bool isDisposing)
        {
            IsDisposed = true;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        public abstract TextureGL Native { get; }

        public abstract bool Loaded { get; }

        public abstract int TextureId { get; }

        public abstract int Height { get; set; }

        public abstract int Width { get; set; }

        public Vector2 Size => new Vector2(Width, Height);

        public abstract RectangleF GetTextureRect(RectangleF? textureRect);

        /// <summary>
        /// Blit a triangle to OpenGL display with specified parameters.
        /// </summary>
        public abstract void DrawTriangle(Triangle vertexTriangle, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null);

        /// <summary>
        /// Blit a quad to OpenGL display with specified parameters.
        /// </summary>
        public abstract void DrawQuad(Quad vertexQuad, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null);

        /// <summary>
        /// Bind as active texture.
        /// </summary>
        /// <returns>True if bind was successful.</returns>
        public abstract bool Bind();

        /// <summary>
        /// Uploads pending texture data to the GPU if it exists.
        /// </summary>
        /// <returns>Whether pending data existed and an upload has been performed.</returns>
        internal abstract bool Upload();

        public abstract void SetData(TextureUpload upload);
    }
}
