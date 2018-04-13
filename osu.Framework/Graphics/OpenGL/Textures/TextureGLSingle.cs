// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using OpenTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal class TextureGLSingle : TextureGL
    {
        public const int MAX_MIPMAP_LEVELS = 3;

        private static readonly Action<TexturedVertex2D> default_quad_action;
        private static readonly Action<TexturedVertex2D> default_triangle_action;

        static TextureGLSingle()
        {
            QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(512, 128);
            default_quad_action = quadBatch.AddAction;

            // We multiply the size param by 3 such that the amount of vertices is a multiple of the amount of vertices
            // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
            // grouping of vertices into primitives.
            LinearBatch<TexturedVertex2D> triangleBatch = new LinearBatch<TexturedVertex2D>(512 * 3, 128, PrimitiveType.Triangles);
            default_triangle_action = triangleBatch.AddAction;
        }

        private readonly ConcurrentQueue<TextureUpload> uploadQueue = new ConcurrentQueue<TextureUpload>();

        private int internalWidth;
        private int internalHeight;

        private readonly All filteringMode;
        private TextureWrapMode internalWrapMode;

        public override bool Loaded => textureId > 0 || uploadQueue.Count > 0;

        public TextureGLSingle(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
        {
            Width = width;
            Height = height;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
        }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            while (uploadQueue.TryDequeue(out TextureUpload u))
                u.Dispose();

            GLWrapper.ScheduleDisposal(unload);
        }

        /// <summary>
        /// Removes texture from GL memory.
        /// </summary>
        private void unload()
        {
            int disposableId = textureId;

            if (disposableId <= 0)
                return;

            GL.DeleteTextures(1, new[] { disposableId });

            textureId = 0;
        }

        #endregion

        private int height;

        public override TextureGL Native => this;

        public override int Height
        {
            get { return height; }
            set { height = value; }
        }

        private int width;

        public override int Width
        {
            get { return width; }
            set { width = value; }
        }

        private int textureId;

        public override int TextureId
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Can not obtain ID of a disposed texture.");

                if (textureId == 0)
                    throw new InvalidOperationException("Can not obtain ID of a texture before uploading it.");

                return textureId;
            }
        }

        private static void rotateVector(ref Vector2 toRotate, float sin, float cos)
        {
            float oldX = toRotate.X;
            toRotate.X = toRotate.X * cos - toRotate.Y * sin;
            toRotate.Y = oldX * sin + toRotate.Y * cos;
        }

        public override RectangleF GetTextureRect(RectangleF? textureRect)
        {
            RectangleF texRect = textureRect != null
                ? new RectangleF(textureRect.Value.X, textureRect.Value.Y, textureRect.Value.Width, textureRect.Value.Height)
                : new RectangleF(0, 0, Width, Height);

            texRect.X /= width;
            texRect.Y /= height;
            texRect.Width /= width;
            texRect.Height /= height;

            return texRect;
        }

        public override void DrawTriangle(Triangle vertexTriangle, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not draw a triangle with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;
            RectangleF inflatedTexRect = texRect.Inflate(inflationAmount);

            if (vertexAction == null)
                vertexAction = default_triangle_action;

            // We split the triangle into two, such that we can obtain smooth edges with our
            // texture coordinate trick. We might want to revert this to drawing a single
            // triangle in case we ever need proper texturing, or if the additional vertices
            // end up becoming an overhead (unlikely).
            SRGBColour topColour = (drawColour.TopLeft + drawColour.TopRight) / 2;
            SRGBColour bottomColour = (drawColour.BottomLeft + drawColour.BottomRight) / 2;

            // Left triangle half
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P0,
                TexturePosition = new Vector2(inflatedTexRect.Left, inflatedTexRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = topColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P1,
                TexturePosition = new Vector2(inflatedTexRect.Left, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = (vertexTriangle.P1 + vertexTriangle.P2) / 2,
                TexturePosition = new Vector2((inflatedTexRect.Left + inflatedTexRect.Right) / 2, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = bottomColour.Linear,
            });

            // Right triangle half
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P0,
                TexturePosition = new Vector2(inflatedTexRect.Right, inflatedTexRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = topColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = (vertexTriangle.P1 + vertexTriangle.P2) / 2,
                TexturePosition = new Vector2((inflatedTexRect.Left + inflatedTexRect.Right) / 2, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = bottomColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P2,
                TexturePosition = new Vector2(inflatedTexRect.Right, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomRight.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexTriangle.ConservativeArea);
        }

        public override void DrawQuad(Quad vertexQuad, RectangleF? textureRect, ColourInfo drawColour, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not draw a quad with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;
            RectangleF inflatedTexRect = texRect.Inflate(inflationAmount);
            Vector2 blendRange = blendRangeOverride ?? inflationAmount;

            if (vertexAction == null)
                vertexAction = default_quad_action;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomLeft,
                TexturePosition = new Vector2(inflatedTexRect.Left, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomRight,
                TexturePosition = new Vector2(inflatedTexRect.Right, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopRight,
                TexturePosition = new Vector2(inflatedTexRect.Right, inflatedTexRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopLeft,
                TexturePosition = new Vector2(inflatedTexRect.Left, inflatedTexRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopLeft.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexQuad.ConservativeArea);
        }

        private void updateWrapMode()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not update wrap mode of a disposed texture.");

            internalWrapMode = WrapMode;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)internalWrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)internalWrapMode);
        }

        public override void SetData(TextureUpload upload)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not set data of a disposed texture.");

            if (upload.Bounds.IsEmpty)
                upload.Bounds = new RectangleI(0, 0, width, height);

            IsTransparent = false;

            bool requireUpload = uploadQueue.Count == 0;
            uploadQueue.Enqueue(upload);
            if (requireUpload)
                GLWrapper.EnqueueTextureUpload(this);
        }

        public override bool Bind()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (textureId <= 0)
                return false;

            if (IsTransparent)
                return false;

            GLWrapper.BindTexture(this);

            if (internalWrapMode != WrapMode)
                updateWrapMode();

            return true;
        }

        private bool manualMipmaps;

        internal override bool Upload()
        {
            // We should never run raw OGL calls on another thread than the main thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not upload data to a disposed texture.");

            bool didUpload = false;

            while (uploadQueue.TryDequeue(out TextureUpload upload))
            {
                IntPtr dataPointer;
                GCHandle? h0;

                if (upload.Data.Length == 0)
                {
                    h0 = null;
                    dataPointer = IntPtr.Zero;
                }
                else
                {
                    h0 = GCHandle.Alloc(upload.Data, GCHandleType.Pinned);
                    dataPointer = h0.Value.AddrOfPinnedObject();
                    didUpload = true;
                }

                try
                {
                    // Do we need to generate a new texture?
                    if (textureId <= 0 || internalWidth != width || internalHeight != height)
                    {
                        internalWidth = width;
                        internalHeight = height;

                        // We only need to generate a new texture if we don't have one already. Otherwise just re-use the current one.
                        if (textureId <= 0)
                        {
                            int[] textures = new int[1];
                            GL.GenTextures(1, textures);

                            textureId = textures[0];

                            GLWrapper.BindTexture(this);
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                                (int)(manualMipmaps ? filteringMode : (filteringMode == All.Linear ? All.LinearMipmapLinear : All.Nearest)));
                            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filteringMode);

                            // 33085 is GL_TEXTURE_MAX_LEVEL, which is not available within TextureParameterName.
                            // It controls the amount of mipmap levels generated by GL.GenerateMipmap later on.
                            GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)33085, MAX_MIPMAP_LEVELS);

                            updateWrapMode();
                        }
                        else
                            GLWrapper.BindTexture(this);

                        if (width == upload.Bounds.Width && height == upload.Bounds.Height || dataPointer == IntPtr.Zero)
                            GL.TexImage2D(TextureTarget2d.Texture2D, upload.Level, TextureComponentCount.Srgb8Alpha8, width, height, 0, upload.Format, PixelType.UnsignedByte, dataPointer);
                        else
                        {
                            initializeLevel(upload.Level, width, height);

                            GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Format, PixelType.UnsignedByte,
                                dataPointer);
                        }
                    }
                    // Just update content of the current texture
                    else if (dataPointer != IntPtr.Zero)
                    {
                        GLWrapper.BindTexture(this);

                        if (!manualMipmaps && upload.Level > 0)
                        {
                            //allocate mipmap levels
                            int level = 1;
                            int d = 2;

                            while (width / d > 0)
                            {
                                initializeLevel(level, width / d, height / d);
                                level++;
                                d *= 2;
                            }

                            manualMipmaps = true;
                        }

                        int div = (int)Math.Pow(2, upload.Level);

                        GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X / div, upload.Bounds.Y / div, upload.Bounds.Width / div, upload.Bounds.Height / div, upload.Format,
                            PixelType.UnsignedByte, dataPointer);
                    }
                }
                finally
                {
                    h0?.Free();
                    upload.Dispose();
                }
            }

            if (didUpload && !manualMipmaps)
            {
                GL.Hint(HintTarget.GenerateMipmapHint, HintMode.Nicest);
                GL.GenerateMipmap(TextureTarget.Texture2D);
            }

            return didUpload;
        }

        private void initializeLevel(int level, int width, int height)
        {
            byte[] transparentWhite = new byte[width * height * 4];
            GCHandle h0 = GCHandle.Alloc(transparentWhite, GCHandleType.Pinned);
            GL.TexImage2D(TextureTarget2d.Texture2D, level, TextureComponentCount.Srgb8Alpha8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, h0.AddrOfPinnedObject());
            h0.Free();
        }
    }
}
