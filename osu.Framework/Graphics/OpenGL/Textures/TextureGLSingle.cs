// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Extensions.ImageExtensions;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Platform;
using osuTK;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal class TextureGLSingle : TextureGL
    {
        /// <summary>
        /// Contains all currently-active <see cref="TextureGLSingle"/>es.
        /// </summary>
        private static readonly LockedWeakList<TextureGLSingle> all_textures = new LockedWeakList<TextureGLSingle>();

        public const int MAX_MIPMAP_LEVELS = 3;

        private static readonly Action<TexturedVertex2D> default_quad_action = new QuadBatch<TexturedVertex2D>(100, 1000).AddAction;

        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        /// <summary>
        /// Invoked when a new <see cref="TextureGLAtlas"/> is created.
        /// </summary>
        /// <remarks>
        /// Invocation from the draw or update thread cannot be assumed.
        /// </remarks>
        public static event Action<TextureGLSingle> TextureCreated;

        private int internalWidth;
        private int internalHeight;

        private readonly All filteringMode;

        private readonly Rgba32 initialisationColour;

        /// <summary>
        /// The total amount of times this <see cref="TextureGLSingle"/> was bound.
        /// </summary>
        public ulong BindCount { get; protected set; }

        // ReSharper disable once InconsistentlySynchronizedField (no need to lock here. we don't really care if the value is stale).
        public override bool Loaded => textureId > 0 || uploadQueue.Count > 0;

        public override RectangleI Bounds => new RectangleI(0, 0, Width, Height);

        /// <summary>
        /// Creates a new <see cref="TextureGLSingle"/>.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public TextureGLSingle(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
            : base(wrapModeS, wrapModeT)
        {
            Width = width;
            Height = height;
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;

            all_textures.Add(this);

            TextureCreated?.Invoke(this);
        }

        /// <summary>
        /// Retrieves all currently-active <see cref="TextureGLSingle"/>s.
        /// </summary>
        public static TextureGLSingle[] GetAllTextures() => all_textures.ToArray();

        #region Disposal

        ~TextureGLSingle()
        {
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            all_textures.Remove(this);

            while (tryGetNextUpload(out var upload))
                upload.Dispose();

            GLWrapper.ScheduleDisposal(texture =>
            {
                int disposableId = texture.textureId;

                if (disposableId <= 0)
                    return;

                GL.DeleteTextures(1, new[] { disposableId });

                texture.memoryLease?.Dispose();

                texture.textureId = 0;
            }, this);
        }

        #endregion

        #region Memory Tracking

        private List<long> levelMemoryUsage = new List<long>();

        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        private void updateMemoryUsage(int level, long newUsage)
        {
            levelMemoryUsage ??= new List<long>();

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

        /// <summary>
        /// Retrieves the size of this texture in bytes.
        /// </summary>
        public virtual int GetByteSize() => Width * Height * 4;

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
                                            Vector2? inflationPercentage = null, RectangleF? textureCoords = null)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not draw a triangle with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;

            // If clamp to edge is active, allow the texture coordinates to penetrate by half the repeated atlas margin width
            if (GLWrapper.CurrentWrapModeS == WrapMode.ClampToEdge || GLWrapper.CurrentWrapModeT == WrapMode.ClampToEdge)
            {
                Vector2 inflationVector = Vector2.Zero;

                const int mipmap_padding_requirement = (1 << MAX_MIPMAP_LEVELS) / 2;

                if (GLWrapper.CurrentWrapModeS == WrapMode.ClampToEdge)
                    inflationVector.X = mipmap_padding_requirement / (float)width;
                if (GLWrapper.CurrentWrapModeT == WrapMode.ClampToEdge)
                    inflationVector.Y = mipmap_padding_requirement / (float)height;
                texRect = texRect.Inflate(inflationVector);
            }

            RectangleF coordRect = GetTextureRect(textureCoords ?? textureRect);
            RectangleF inflatedCoordRect = coordRect.Inflate(inflationAmount);

            vertexAction ??= default_quad_action;

            // We split the triangle into two, such that we can obtain smooth edges with our
            // texture coordinate trick. We might want to revert this to drawing a single
            // triangle in case we ever need proper texturing, or if the additional vertices
            // end up becoming an overhead (unlikely).
            SRGBColour topColour = (drawColour.TopLeft + drawColour.TopRight) / 2;
            SRGBColour bottomColour = (drawColour.BottomLeft + drawColour.BottomRight) / 2;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P0,
                TexturePosition = new Vector2((inflatedCoordRect.Left + inflatedCoordRect.Right) / 2, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = topColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P1,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = (vertexTriangle.P1 + vertexTriangle.P2) / 2,
                TexturePosition = new Vector2((inflatedCoordRect.Left + inflatedCoordRect.Right) / 2, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = bottomColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P2,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomRight.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexTriangle.Area);
        }

        public const int VERTICES_PER_QUAD = 4;

        internal override void DrawQuad(Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D> vertexAction = null, Vector2? inflationPercentage = null,
                                        Vector2? blendRangeOverride = null, RectangleF? textureCoords = null)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not draw a quad with a disposed texture.");

            RectangleF texRect = GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;

            // If clamp to edge is active, allow the texture coordinates to penetrate by half the repeated atlas margin width
            if (GLWrapper.CurrentWrapModeS == WrapMode.ClampToEdge || GLWrapper.CurrentWrapModeT == WrapMode.ClampToEdge)
            {
                Vector2 inflationVector = Vector2.Zero;

                const int mipmap_padding_requirement = (1 << MAX_MIPMAP_LEVELS) / 2;

                if (GLWrapper.CurrentWrapModeS == WrapMode.ClampToEdge)
                    inflationVector.X = mipmap_padding_requirement / (float)width;
                if (GLWrapper.CurrentWrapModeT == WrapMode.ClampToEdge)
                    inflationVector.Y = mipmap_padding_requirement / (float)height;
                texRect = texRect.Inflate(inflationVector);
            }

            RectangleF coordRect = GetTextureRect(textureCoords ?? textureRect);
            RectangleF inflatedCoordRect = coordRect.Inflate(inflationAmount);
            Vector2 blendRange = blendRangeOverride ?? inflationAmount;

            vertexAction ??= default_quad_action;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomLeft,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomRight,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopRight,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopLeft,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopLeft.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexQuad.Area);
        }

        internal override void SetData(ITextureUpload upload, WrapMode wrapModeS, WrapMode wrapModeT, Opacity? uploadOpacity)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not set data of a disposed texture.");

            if (upload.Bounds.IsEmpty && upload.Data.Length > 0)
            {
                upload.Bounds = Bounds;
                if (width * height > upload.Data.Length)
                    throw new InvalidOperationException($"Size of texture upload ({width}x{height}) does not contain enough data ({upload.Data.Length} < {width * height})");
            }

            UpdateOpacity(upload, ref uploadOpacity);

            lock (uploadQueue)
            {
                bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);

                if (requireUpload && !BypassTextureUploadQueueing)
                    GLWrapper.EnqueueTextureUpload(this);
            }
        }

        internal override bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (textureId <= 0)
                return false;

            if (GLWrapper.BindTexture(this, unit, wrapModeS, wrapModeT))
                BindCount++;

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
            {
                using (upload)
                {
                    fixed (Rgba32* ptr = upload.Data)
                        DoUpload(upload, (IntPtr)ptr);

                    didUpload = true;
                }
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

        protected virtual void DoUpload(ITextureUpload upload, IntPtr dataPointer)
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

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, MAX_MIPMAP_LEVELS);

                    // These shouldn't be required, but on some older Intel drivers the MAX_LOD chosen by the shader isn't clamped to the MAX_LEVEL from above, resulting in disappearing textures.
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, MAX_MIPMAP_LEVELS);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                        (int)(manualMipmaps ? filteringMode : filteringMode == All.Linear ? All.LinearMipmapLinear : All.Nearest));
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filteringMode);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
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

        private void initializeLevel(int level, int width, int height)
        {
            using (var image = createBackingImage(width, height))
            using (var pixels = image.CreateReadOnlyPixelSpan())
            {
                updateMemoryUsage(level, (long)width * height * 4);
                GL.TexImage2D(TextureTarget2d.Texture2D, level, TextureComponentCount.Srgb8Alpha8, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte,
                    ref MemoryMarshal.GetReference(pixels.Span));
            }
        }

        private Image<Rgba32> createBackingImage(int width, int height)
        {
            // it is faster to initialise without a background specification if transparent black is all that's required.
            return initialisationColour == default
                ? new Image<Rgba32>(width, height)
                : new Image<Rgba32>(width, height, initialisationColour);
        }
    }
}
