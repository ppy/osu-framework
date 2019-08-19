// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using osuTK;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;

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

        internal int ReferenceCount;

        public void Reference() => Interlocked.Increment(ref ReferenceCount);

        public void Dereference()
        {
            if (Interlocked.Decrement(ref ReferenceCount) == 0)
                Dispose();
        }

        /// <summary>
        /// Whether this <see cref="TextureGL"/> can used for drawing.
        /// </summary>
        public bool Available { get; private set; } = true;

        private bool isDisposed;

        protected virtual void Dispose(bool isDisposing) => GLWrapper.ScheduleDisposal(() => Available = false);

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

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
        /// Draws a triangle to the screen.
        /// </summary>
        /// <param name="vertexTriangle">The triangle to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        internal abstract void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                            Vector2? inflationPercentage = null);

        /// <summary>
        /// Draws a quad to the screen.
        /// </summary>
        /// <param name="vertexQuad">The quad to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <see cref="textureRect"/> should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the <see cref="textureRect"/> should be blended.</param>
        internal abstract void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null);

        /// <summary>
        /// Bind as active texture.
        /// </summary>
        /// <param name="unit">The texture unit to bind to. Defaults to Texture0.</param>
        /// <returns>True if bind was successful.</returns>
        public abstract bool Bind(TextureUnit unit = TextureUnit.Texture0);

        /// <summary>
        /// Uploads pending texture data to the GPU if it exists.
        /// </summary>
        /// <returns>Whether pending data existed and an upload has been performed.</returns>
        internal abstract bool Upload();

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        internal abstract void FlushUploads();

        public abstract void SetData(ITextureUpload upload);
    }
}
