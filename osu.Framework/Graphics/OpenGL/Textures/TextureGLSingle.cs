// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal class TextureGLSingle : TextureGL
    {
        public const int MAX_MIPMAP_LEVELS = 3;

        private static readonly Action<TexturedVertex2D> default_quad_action;

        static TextureGLSingle()
        {
            QuadBatch<TexturedVertex2D> quadBatch = new QuadBatch<TexturedVertex2D>(100, 1000);
            default_quad_action = quadBatch.AddAction;
        }

        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        private int internalWidth;
        private int internalHeight;

        private readonly All filteringMode;
        private TextureWrapMode internalWrapMode;

        // ReSharper disable once InconsistentlySynchronizedField (no need to lock here. we don't really care if the value is stale).
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

            while (tryGetNextUpload(out var upload))
                upload.Dispose();

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

            memoryLease?.Dispose();
        }

        #endregion

        #region Memory Tracking

        private List<long> levelMemoryUsage = new List<long>();

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        private void updateMemoryUsage(int level, long newUsage)
        {
            if (levelMemoryUsage == null)
                levelMemoryUsage = new List<long>();

            while (level >= levelMemoryUsage.Count)
                levelMemoryUsage.Add(0);

            levelMemoryUsage[level] = newUsage;

            memoryLease?.Dispose();
            memoryLease = NativeMemoryTracker.AddMemory(this, getMemoryUsage());
        }

        private long getMemoryUsage()
        {
            long usage = 0;

            for (int i = 0; i < levelMemoryUsage.Count; i++)
                usage += levelMemoryUsage[i];

            return usage;
        }

        #endregion

        private int height;

        public override TextureGL Native => this;

        public override int Height
        {
            get => height;
            set => height = value;
        }

        private int width;

        public override int Width
        {
            get => width;
            set => width = value;
        }

        private int textureId;

        public override int TextureId
        {
            get
            {
                if (!Available)
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

        public const int VERTICES_PER_TRIANGLE = 4;

        internal override void DrawTriangle(Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null,
                                            Vector2? inflationPercentage = null)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not draw a triangle with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;
            RectangleF inflatedTexRect = texRect.Inflate(inflationAmount);

            if (vertexAction == null)
                vertexAction = default_quad_action;

            // We split the triangle into two, such that we can obtain smooth edges with our
            // texture coordinate trick. We might want to revert this to drawing a single
            // triangle in case we ever need proper texturing, or if the additional vertices
            // end up becoming an overhead (unlikely).
            SRGBColour topColour = (drawColour.TopLeft + drawColour.TopRight) / 2;
            SRGBColour bottomColour = (drawColour.BottomLeft + drawColour.BottomRight) / 2;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P0,
                TexturePosition = new Vector2((inflatedTexRect.Left + inflatedTexRect.Right) / 2, inflatedTexRect.Top),
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
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P2,
                TexturePosition = new Vector2(inflatedTexRect.Right, inflatedTexRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomRight.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexTriangle.Area);
        }

        public const int VERTICES_PER_QUAD = 4;

        internal override void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null)
        {
            if (!Available)
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

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexQuad.Area);
        }

        private void updateWrapMode()
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not update wrap mode of a disposed texture.");

            internalWrapMode = WrapMode;
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)internalWrapMode);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)internalWrapMode);
        }

        public override void SetData(ITextureUpload upload)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not set data of a disposed texture.");

            if (upload.Bounds.IsEmpty && upload.Data.Length > 0)
            {
                upload.Bounds = new RectangleI(0, 0, width, height);
                if (width * height > upload.Data.Length)
                    throw new InvalidOperationException($"Size of texture upload ({width}x{height}) does not contain enough data ({upload.Data.Length} < {width * height})");
            }

            IsTransparent = false;

            lock (uploadQueue)
            {
                bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);
                if (requireUpload)
                    GLWrapper.EnqueueTextureUpload(this);
            }
        }

        public override bool Bind(TextureUnit unit = TextureUnit.Texture0)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (textureId <= 0)
                return false;

            if (IsTransparent)
                return false;

            GLWrapper.BindTexture(this, unit);

            if (internalWrapMode != WrapMode)
                updateWrapMode();

            return true;
        }

        private bool manualMipmaps;

        internal override unsafe bool Upload()
        {
            if (!Available)
                return false;

            // We should never run raw OGL calls on another thread than the main thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            bool didUpload = false;

            while (tryGetNextUpload(out ITextureUpload upload))
                using (upload)
                {
                    fixed (Rgba32* ptr = upload.Data)
                        doUpload(upload, (IntPtr)ptr);

                    didUpload = true;
                }

            if (didUpload && !manualMipmaps)
            {
                GL.Hint(HintTarget.GenerateMipmapHint, HintMode.Nicest);
                GL.GenerateMipmap(TextureTarget.Texture2D);
            }

            return didUpload;
        }

        internal override void FlushUploads()
        {
            while (tryGetNextUpload(out var upload))
                upload.Dispose();
        }

        private bool tryGetNextUpload(out ITextureUpload upload)
        {
            lock (uploadQueue)
            {
                if (uploadQueue.Count == 0)
                {
                    upload = null;
                    return false;
                }

                upload = uploadQueue.Dequeue();
                return true;
            }
        }

        private void doUpload(ITextureUpload upload, IntPtr dataPointer)
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
                        (int)(manualMipmaps ? filteringMode : filteringMode == All.Linear ? All.LinearMipmapLinear : All.Nearest));
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filteringMode);

                    // 33085 is GL_TEXTURE_MAX_LEVEL, which is not available within TextureParameterName.
                    // It controls the amount of mipmap levels generated by GL.GenerateMipmap later on.
                    GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)33085, MAX_MIPMAP_LEVELS);

                    updateWrapMode();
                }
                else
                    GLWrapper.BindTexture(this);

                if (width == upload.Bounds.Width && height == upload.Bounds.Height || dataPointer == IntPtr.Zero)
                {
                    updateMemoryUsage(upload.Level, (long)width * height * 4);
                    GL.TexImage2D(TextureTarget2d.Texture2D, upload.Level, TextureComponentCount.Srgb8Alpha8, width, height, 0, upload.Format, PixelType.UnsignedByte, dataPointer);
                }
                else
                {
                    initializeLevel(upload.Level, width, height);

                    GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Format,
                        PixelType.UnsignedByte, dataPointer);
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

                GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X / div, upload.Bounds.Y / div, upload.Bounds.Width / div, upload.Bounds.Height / div,
                    upload.Format, PixelType.UnsignedByte, dataPointer);
            }
        }

        private unsafe void initializeLevel(int level, int width, int height)
        {
            using (var image = new Image<Rgba32>(width, height))
                fixed (void* buffer = &MemoryMarshal.GetReference(image.GetPixelSpan()))
                {
                    updateMemoryUsage(level, (long)width * height * 4);
                    GL.TexImage2D(TextureTarget2d.Texture2D, level, TextureComponentCount.Srgb8Alpha8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (IntPtr)buffer);
                }
        }
    }
}
