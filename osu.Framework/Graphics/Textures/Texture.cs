// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
        private static TextureWhitePixel whitePixel;

        public static Texture WhitePixel
        {
            get
            {
                if (whitePixel == null)
                {
                    TextureAtlas atlas = new TextureAtlas(3, 3, true);
                    whitePixel = atlas.GetWhitePixel();
                    whitePixel.SetData(new TextureUpload(new byte[] { 255, 255, 255, 255 }));
                }

                return whitePixel;
            }
        }

        public TextureGL TextureGL;
        public string Filename;
        public string AssetName;

        /// <summary>
        /// At what multiple of our expected resolution is our underlying texture?
        /// </summary>
        public float ScaleAdjust = 1;

        public bool Disposable = true;
        public bool IsDisposed { get; private set; }

        public float DisplayWidth => Width / ScaleAdjust;
        public float DisplayHeight => Height / ScaleAdjust;

        public Texture(TextureGL textureGl)
        {
            if (textureGl == null)
                throw new ArgumentNullException(nameof(textureGl));
            TextureGL = textureGl;
        }

        public Texture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
            : this(new TextureGLSingle(width, height, manualMipmaps, filteringMode))
        {
        }

        #region Disposal

        ~Texture()
        {
            Dispose(false);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }

        #endregion

        public int Width
        {
            get { return TextureGL.Width; }
            set { TextureGL.Width = value; }
        }

        public int Height
        {
            get { return TextureGL.Height; }
            set { TextureGL.Height = value; }
        }

        public Vector2 Size => new Vector2(Width, Height);

        /// <summary>
        /// Turns a byte array representing BGRA colour values to a byte array representing RGBA colour values.
        /// Checks whether all colour values are transparent as a byproduct.
        /// </summary>
        /// <param name="data">The bytes to process.</param>
        /// <param name="length">The amount of bytes to process.</param>
        /// <returns>Whether all colour values are transparent.</returns>
        private static unsafe bool bgraToRgba(byte[] data, int length)
        {
            bool isTransparent = true;

            fixed (byte* dPtr = &data[0])
            {
                byte* sp = dPtr;
                byte* ep = dPtr + length;

                while (sp < ep)
                {
                    *(uint*)sp = (uint)(*(sp + 2) | *(sp + 1) << 8 | *sp << 16 | *(sp + 3) << 24);
                    isTransparent &= *(sp + 3) == 0;
                    sp += 4;
                }
            }

            return isTransparent;
        }

        public void SetData(TextureUpload upload)
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
    }
}
