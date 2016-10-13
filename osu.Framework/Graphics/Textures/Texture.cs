﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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

namespace osu.Framework.Graphics.Textures
{
    public class Texture : IDisposable
    {
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

        public Texture(int width, int height)
            : this(new TextureGLSingle(width, height))
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

            if (TextureGL != null)
            {
                TextureGL.Dispose();
                TextureGL = null;
            }
        }

        #endregion

        public int Width
        {
            get
            {
                Debug.Assert(TextureGL != null);
                return TextureGL?.Width ?? 0;
            }
            set { TextureGL.Width = value; }
        }

        public int Height
        {
            get
            {
                Debug.Assert(TextureGL != null);
                return TextureGL?.Height ?? 0;
            }
            set { TextureGL.Height = value; }
        }

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

            BitmapData bData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

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

        public void Draw(Quad vertexQuad, Color4 colour, RectangleF? textureRect = null, VertexBatch<TexturedVertex2d> spriteBatch = null)
        {
            RectangleF texRect = textureRect ?? new RectangleF(0, 0, DisplayWidth, DisplayHeight);

            if (DpiScale != 1)
            {
                texRect.Width *= DpiScale;
                texRect.Height *= DpiScale;
                texRect.X *= DpiScale;
                texRect.Y *= DpiScale;
            }

            TextureGL?.Draw(vertexQuad, texRect, colour, spriteBatch);
        }
    }
}
