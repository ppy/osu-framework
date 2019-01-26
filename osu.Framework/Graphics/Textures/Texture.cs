// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
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
        private static readonly Lazy<TextureWhitePixel> white_pixel = new Lazy<TextureWhitePixel>(() => new TextureAtlas(3, 3, true).WhitePixel);

        public static Texture WhitePixel => white_pixel.Value;

        public virtual TextureGL TextureGL { get; }

        public string Filename;
        public string AssetName;

        /// <summary>
        /// At what multiple of our expected resolution is our underlying texture?
        /// </summary>
        public float ScaleAdjust = 1;

        public float DisplayWidth => Width / ScaleAdjust;
        public float DisplayHeight => Height / ScaleAdjust;

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

        public RectangleF GetTextureRect(RectangleF? textureRect = null)
        {
            return TextureGL.GetTextureRect(TextureBounds(textureRect));
        }

        public void DrawTriangle(Triangle vertexTriangle, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawTriangle(vertexTriangle, TextureBounds(textureRect), colour, vertexAction, inflationPercentage);
        }

        public void DrawQuad(Quad vertexQuad, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawQuad(vertexQuad, TextureBounds(textureRect), colour, vertexAction, inflationPercentage, blendRangeOverride);
        }

        public override string ToString() => $@"{AssetName} ({Width}, {Height})";

        /// <summary>
        /// Whether <see cref="TextureGL"/> is in a usable state.
        /// </summary>
        public virtual bool Available => !TextureGL.IsDisposed;

        #region Disposal

        // Intentionally no finalizer implementation as our disposal is NOOP. Finalizer is implemented in TextureWithRefCount usage.

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool isDisposing)
        {
        }

        #endregion
    }
}
