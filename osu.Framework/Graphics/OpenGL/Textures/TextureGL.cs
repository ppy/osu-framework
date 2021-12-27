// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        /// <summary>
        /// The texture wrap mode in horizontal direction.
        /// </summary>
        public readonly WrapMode WrapModeS;

        /// <summary>
        /// The texture wrap mode in vertical direction.
        /// </summary>
        public readonly WrapMode WrapModeT;

        protected TextureGL(WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            WrapModeS = wrapModeS;
            WrapModeT = wrapModeT;
        }

        #region Disposal

        internal virtual bool IsQueuedForUpload { get; set; }

        /// <summary>
        /// By default, texture uploads are queued for upload at the beginning of each frame, allowing loading them ahead of time.
        /// When this is true, this will be bypassed and textures will only be uploaded on use. Should be set for every-frame texture uploads
        /// to avoid overloading the global queue.
        /// </summary>
        public bool BypassTextureUploadQueueing;

        /// <summary>
        /// Whether this <see cref="TextureGL"/> can used for drawing.
        /// </summary>
        public bool Available { get; private set; } = true;

        private bool isDisposed;

        protected virtual void Dispose(bool isDisposing)
        {
            GLWrapper.ScheduleDisposal(t => t.Available = false, this);
        }

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

        public Opacity Opacity { get; protected set; } = Opacity.Mixed;

        public abstract int TextureId { get; }

        public abstract int Height { get; set; }

        public abstract int Width { get; set; }

        public abstract RectangleI Bounds { get; }

        public Vector2 Size => new Vector2(Width, Height);

        public abstract RectangleF GetTextureRect(RectangleF? textureRect);

        /// <summary>
        /// Draws a triangle to the screen.
        /// </summary>
        /// <param name="vertexTriangle">The triangle to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="textureCoords">The texture coordinates of the triangle's vertices (translated from the corresponding quad's rectangle).</param>
        internal abstract void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                            Vector2? inflationPercentage = null, RectangleF? textureCoords = null);

        /// <summary>
        /// Draws a quad to the screen.
        /// </summary>
        /// <param name="vertexQuad">The quad to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the <paramref name="textureRect"/> should be blended.</param>
        /// <param name="textureCoords">The texture coordinates of the quad's vertices.</param>
        internal abstract void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null, RectangleF? textureCoords = null);

        /// <summary>
        /// Bind as active texture.
        /// </summary>
        /// <param name="unit">The texture unit to bind to. Defaults to Texture0.</param>
        /// <returns>True if bind was successful.</returns>
        public bool Bind(TextureUnit unit = TextureUnit.Texture0) => Bind(unit, WrapModeS, WrapModeT);

        /// <summary>
        /// Bind as active texture.
        /// </summary>
        /// <param name="unit">The texture unit to bind to.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>True if bind was successful.</returns>
        internal abstract bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT);

        /// <summary>
        /// Uploads pending texture data to the GPU if it exists.
        /// </summary>
        /// <returns>Whether pending data existed and an upload has been performed.</returns>
        internal abstract bool Upload();

        /// <summary>
        /// Flush any unprocessed uploads without actually uploading.
        /// </summary>
        internal abstract void FlushUploads();

        /// <summary>
        /// Sets the pixel data of this <see cref="TextureGL"/>.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        public void SetData(ITextureUpload upload) => SetData(upload, WrapModeS, WrapModeT, null);

        /// <summary>
        /// Sets the pixel data of this <see cref="TextureGLAtlas"/>.
        /// </summary>
        /// <param name="upload">The <see cref="ITextureUpload"/> containing the data.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="uploadOpacity">Whether the upload is opaque, transparent, or a mix of both..</param>
        internal abstract void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity);

        protected static Opacity ComputeOpacity(ITextureUpload upload)
        {
            // TODO: Investigate performance issues and revert functionality once we are sure there is no overhead.
            // see https://github.com/ppy/osu/issues/9307
            return Opacity.Mixed;

            // if (upload.Data.Length == 0)
            //     return Opacity.Transparent;
            //
            // bool isTransparent = true;
            // bool isOpaque = true;
            //
            // for (int i = 0; i < upload.Data.Length; ++i)
            // {
            //     isTransparent &= upload.Data[i].A == 0;
            //     isOpaque &= upload.Data[i].A == 255;
            //
            //     if (!isTransparent && !isOpaque)
            //         return Opacity.Mixed;
            // }
            //
            // if (isTransparent)
            //     return Opacity.Transparent;
            //
            // return Opacity.Opaque;
        }

        protected void UpdateOpacity(ITextureUpload upload, ref Opacity? uploadOpacity)
        {
            // Compute opacity if it doesn't have a value yet
            uploadOpacity ??= ComputeOpacity(upload);

            // Update the texture's opacity depending on the upload's opacity.
            // If the upload covers the entire bounds of the texture, it fully
            // determines the texture's opacity. Otherwise, it can only turn
            // the texture's opacity into a mixed state (if it disagrees with
            // the texture's existing opacity).
            if (upload.Bounds == Bounds && upload.Level == 0)
                Opacity = uploadOpacity.Value;
            else if (uploadOpacity.Value != Opacity)
                Opacity = Opacity.Mixed;
        }
    }

    public enum WrapMode
    {
        /// <summary>
        /// No wrapping. If the texture is part of an atlas, this may read outside the texture's bounds.
        /// </summary>
        None = 0,

        /// <summary>
        /// Clamps to the edge of the texture, repeating the edge to fill the remainder of the draw area.
        /// </summary>
        ClampToEdge = 1,

        /// <summary>
        /// Clamps to a transparent-black border around the texture, repeating the border to fill the remainder of the draw area.
        /// </summary>
        ClampToBorder = 2,

        /// <summary>
        /// Repeats the texture.
        /// </summary>
        Repeat = 3,
    }

    public enum Opacity
    {
        Opaque,
        Mixed,
        Transparent,
    }
}
