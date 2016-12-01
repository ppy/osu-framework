// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using OpenTK.Graphics;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using OpenTK;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
        private static Texture whitePixel;
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
        public float DpiScale = 1;
        public bool Disposable = true;
        public bool IsDisposed { get; private set; }

        public float DisplayWidth => Width / DpiScale;
        public float DisplayHeight => Height / DpiScale;

        public Texture(TextureGL textureGl)
        {
            Debug.Assert(textureGl != null);
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

        private void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
        }

        #endregion

        public int Width
        {
            get
            {
                Debug.Assert(TextureGL != null);
                return TextureGL.Width;
            }
            set { TextureGL.Width = value; }
        }

        public int Height
        {
            get
            {
                Debug.Assert(TextureGL != null);
                return TextureGL.Height;
            }
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

        public unsafe void SetData(Bitmap bitmap, int level = 0)
        {
            if (TextureGL == null)
                return;

            int width = Math.Min(bitmap.Width, Width);
            int height = Math.Min(bitmap.Height, Height);

            BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            TextureUpload upload = new TextureUpload(width * height * 4)
            {
                Level = level,
                Bounds = new Rectangle(0, 0, width, height)
            };

            byte[] data = upload.Data;

            int bytesPerPixel = 4;
            byte* bDataPointer = (byte*)bData.Scan0;

            for (var y = 0; y < height; y++)
            {
                // This is why real scan-width is important to have!
                IntPtr row = new IntPtr(bDataPointer + y * bData.Stride);
                Marshal.Copy(row, data, width * bytesPerPixel * y, width * bytesPerPixel);
            }

            bitmap.UnlockBits(bData);

            bool isTransparent = bgraToRgba(data, width * height * 4);
            TextureGL.IsTransparent = isTransparent;

            if (!isTransparent)
                SetData(upload);
            else
                upload.Dispose();
        }

        protected virtual RectangleF textureBounds(RectangleF? textureRect = null)
        {
            RectangleF texRect = textureRect ?? new RectangleF(0, 0, DisplayWidth, DisplayHeight);

            if (DpiScale != 1)
            {
                texRect.Width *= DpiScale;
                texRect.Height *= DpiScale;
                texRect.X *= DpiScale;
                texRect.Y *= DpiScale;
            }

            return texRect;
        }

        public RectangleF GetTextureRect(RectangleF? textureRect = null)
        {
            return TextureGL.GetTextureRect(textureBounds(textureRect));
        }

        public void DrawTriangle(Triangle vertexTriangle, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawTriangle(vertexTriangle, textureBounds(textureRect), colour, vertexAction, inflationPercentage);
        }

        public void DrawQuad(Quad vertexQuad, ColourInfo colour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            if (TextureGL == null || !TextureGL.Bind()) return;

            TextureGL.DrawQuad(vertexQuad, textureBounds(textureRect), colour, vertexAction, inflationPercentage);
        }
    }

    public class TextureWhitePixel : Texture
    {
        public TextureWhitePixel(TextureGL textureGl)
            : base(textureGl)
        {
        }

        protected override RectangleF textureBounds(RectangleF? textureRect = default(RectangleF?))
        {
            return new RectangleF();
        }
    }
}
