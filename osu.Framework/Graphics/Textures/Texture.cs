// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osuTK;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
        // in case no other textures are used in the project, create a new atlas as a fallback source for the white pixel area (used to draw boxes etc.)
        private static readonly Lazy<TextureWhitePixel> white_pixel = new Lazy<TextureWhitePixel>(() =>
            new TextureAtlas(TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, true).WhitePixel);

        public static Texture WhitePixel => white_pixel.Value;

        public virtual TextureGL TextureGL { get; }

        public string Filename;
        public string AssetName;

        /// <summary>
        /// A lookup key used by <see cref="TextureStore"/>s.
        /// </summary>
        internal string LookupKey;

        /// <summary>
        /// At what multiple of our expected resolution is our underlying texture?
        /// </summary>
        public float ScaleAdjust = 1;

        public float DisplayWidth => Width / ScaleAdjust;
        public float DisplayHeight => Height / ScaleAdjust;

        public Opacity Opacity => TextureGL.Opacity;

        public WrapMode WrapModeS => TextureGL.WrapModeS;

        public WrapMode WrapModeT => TextureGL.WrapModeT;

        /// <summary>
        /// Create a new texture.
        /// </summary>
        /// <param name="textureGl">The GL texture.</param>
        public Texture(TextureGL textureGl)
        {
            TextureGL = textureGl ?? throw new ArgumentNullException(nameof(textureGl));
        }

        public Texture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
            : this(new TextureGLSingle(width, height, manualMipmaps, filteringMode))
        {
        }

        /// <summary>
        /// Crop the texture.
        /// </summary>
        /// <param name="cropRectangle">The rectangle the cropped texture should reference.</param>
        /// <param name="relativeSizeAxes">Which axes have a relative size in [0,1] in relation to the texture size.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>The cropped texture.</returns>
        public Texture Crop(RectangleF cropRectangle, Axes relativeSizeAxes = Axes.None, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (relativeSizeAxes != Axes.None)
            {
                Vector2 scale = new Vector2(
                    relativeSizeAxes.HasFlagFast(Axes.X) ? Width : 1,
                    relativeSizeAxes.HasFlagFast(Axes.Y) ? Height : 1
                );
                cropRectangle *= scale;
            }

            return new Texture(new TextureGLSub(cropRectangle, TextureGL, wrapModeS, wrapModeT));
        }

        /// <summary>
        /// Creates a texture from a data stream representing a bitmap.
        /// </summary>
        /// <param name="stream">The data stream containing the texture data.</param>
        /// <param name="atlas">The atlas to add the texture to.</param>
        /// <returns>The created texture.</returns>
        public static Texture FromStream(Stream stream, TextureAtlas atlas = null)
        {
            if (stream == null || stream.Length == 0)
                return null;

            try
            {
                var data = new TextureUpload(stream);
                Texture tex = atlas == null ? new Texture(data.Width, data.Height) : new Texture(atlas.Add(data.Width, data.Height));
                tex.SetData(data);
                return tex;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public int Width
        {
            get => TextureGL.Width;
            set => TextureGL.Width = value;
        }

        public int Height
        {
            get => TextureGL.Height;
            set => TextureGL.Height = value;
        }

        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Queue a <see cref="TextureUpload"/> to be uploaded on the draw thread.
        /// The provided upload will be disposed after the upload is completed.
        /// </summary>
        /// <param name="upload"></param>
        public void SetData(ITextureUpload upload)
        {
            TextureGL?.SetData(upload);
        }

        protected virtual RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            RectangleF texRect = textureRect ?? new RectangleF(0, 0, DisplayWidth, DisplayHeight);

            if (ScaleAdjust != 1)
            {
                texRect.Width *= ScaleAdjust;
                texRect.Height *= ScaleAdjust;
                texRect.X *= ScaleAdjust;
                texRect.Y *= ScaleAdjust;
            }

            return texRect;
        }

        public RectangleF GetTextureRect(RectangleF? textureRect = null) => TextureGL.GetTextureRect(TextureBounds(textureRect));

        /// <summary>
        /// Draws a triangle to the screen.
        /// </summary>
        /// <param name="vertexTriangle">The triangle to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle in texture space.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="textureCoords">The texture coordinates of the triangle's vertices (translated from the corresponding quad's rectangle).</param>
        internal void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                   Vector2? inflationPercentage = null, RectangleF? textureCoords = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawTriangle(vertexTriangle, drawColour, TextureBounds(textureRect), vertexAction, inflationPercentage, TextureBounds(textureCoords));
        }

        /// <summary>
        /// Draws a quad to the screen.
        /// </summary>
        /// <param name="vertexQuad">The quad to draw.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="textureRect">The texture rectangle in texture space.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the <paramref name="textureRect"/> should be blended.</param>
        /// <param name="textureCoords">The texture coordinates of the quad's vertices.</param>
        internal void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                               Vector2? blendRangeOverride = null, RectangleF? textureCoords = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawQuad(vertexQuad, drawColour, TextureBounds(textureRect), vertexAction, inflationPercentage, blendRangeOverride, TextureBounds(textureCoords));
        }

        public override string ToString() => $@"{AssetName} ({Width}, {Height})";

        /// <summary>
        /// Whether <see cref="TextureGL"/> is in a usable state.
        /// </summary>
        public virtual bool Available => TextureGL.Available;

        #region Disposal

        // Intentionally no finalizer implementation as our disposal is NOOP. Finalizer is implemented in TextureWithRefCount usage.

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }

        #endregion
    }
}
