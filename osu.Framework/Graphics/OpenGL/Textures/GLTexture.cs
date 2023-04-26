// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Platform;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    internal class GLTexture : INativeTexture
    {
        protected readonly GLRenderer Renderer;
        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        IRenderer INativeTexture.Renderer => Renderer;

        public string Identifier
        {
            get
            {
                if (!Available || textureId == 0)
                    return "-";

                return textureId.ToString();
            }
        }

        public int MaxSize => Renderer.MaxTextureSize;

        public virtual int Width { get; set; }
        public virtual int Height { get; set; }

        public virtual int GetByteSize() => Width * Height * 4;
        public bool Available { get; private set; } = true;

        private int? mipLevel;

        public int? MipLevel
        {
            get => mipLevel;
            set
            {
                mipLevel = value;

                GL.BindTexture(TextureTarget.Texture2D, textureId);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, mipLevel ?? 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, mipLevel ?? IRenderer.MAX_MIPMAP_LEVELS);
            }
        }

        ulong INativeTexture.TotalBindCount { get; set; }

        public bool BypassTextureUploadQueueing { get; set; }

        private int internalWidth;
        private int internalHeight;

        private readonly All filteringMode;
        private readonly Color4 initialisationColour;
        private readonly bool manualMipmaps;

        /// <summary>
        /// Creates a new <see cref="GLTexture"/>.
        /// </summary>
        /// <param name="renderer"></param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public GLTexture(GLRenderer renderer, int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear, Color4 initialisationColour = default)
        {
            Renderer = renderer;
            Width = width;
            Height = height;

            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;
        }

        #region Disposal

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~GLTexture()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            Renderer.ScheduleDisposal(texture =>
            {
                while (texture.tryGetNextUpload(out var upload))
                    upload.Dispose();

                int disposableId = texture.textureId;

                if (disposableId <= 0)
                    return;

                GL.DeleteTextures(1, new[] { disposableId });

                texture.memoryLease?.Dispose();

                texture.textureId = 0;
                texture.Available = false;
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

        private int textureId;

        public virtual int TextureId
        {
            get
            {
                if (!Available)
                    throw new ObjectDisposedException(ToString(), "Can not obtain ID of a disposed texture.");

                return textureId;
            }
        }

        public void FlushUploads()
        {
            while (tryGetNextUpload(out var upload))
                upload.Dispose();
        }

        public void SetData(ITextureUpload upload)
        {
            lock (uploadQueue)
            {
                bool requireUpload = uploadQueue.Count == 0;
                uploadQueue.Enqueue(upload);

                if (requireUpload && !BypassTextureUploadQueueing)
                    Renderer.EnqueueTextureUpload(this);
            }
        }

        /// <summary>
        /// Represents all regions of uploaded data from the last <see cref="Upload"/> call.
        /// </summary>
        private readonly SortedList<RectangleI> uploadedRegions = new SortedList<RectangleI>((a, b) => a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X));

        public unsafe bool Upload()
        {
            if (!Available)
                return false;

            // We should never run raw OGL calls on another thread than the main thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            uploadedRegions.Clear();

            while (tryGetNextUpload(out ITextureUpload upload))
            {
                using (upload)
                {
                    fixed (Rgba32* ptr = upload.Data)
                        DoUpload(upload, (IntPtr)ptr);

                    uploadedRegions.Add(upload.Bounds);
                }
            }

            GenerateMipmaps();
            return uploadedRegions.Count != 0;
        }

        public bool GenerateMipmaps()
        {
            if (manualMipmaps || uploadedRegions.Count == 0)
                return false;

            var regions = uploadedRegions.ToList();

            // Generate mipmaps for just the updated regions of the texture.
            // This implementation is functionally equivalent to GL.GenerateMipmap(),
            // only that it is much more efficient if only small parts of the texture
            // have been updated.
            RectangleI? current = null;

            RectangleI rightRectangle = regions[0];
            RectangleI? leftRectangle = null;

            int initialX = rightRectangle.X;

            for (int i = 1; i < regions.Count; i++)
            {
                var region = regions[i];

                bool finalElement = i == regions.Count - 1;
                bool finalInRow = !finalElement && regions[i + 1].Y > current?.Y;

                if (region.X >= initialX)
                {
                    current = current == null ? region : RectangleI.Union(current.Value, region);
                    regions.RemoveAt(i--);

                    if (finalInRow)
                        rightRectangle = RectangleI.Union(rightRectangle, current.Value);
                    else if (finalElement && leftRectangle != null)
                        leftRectangle = RectangleI.Union(leftRectangle.Value, current.Value);
                }
                else
                {
                    leftRectangle = leftRectangle == null ? region : RectangleI.Union(leftRectangle.Value, region);
                    regions.RemoveAt(i--);
                }
            }

            if (leftRectangle != null)
                Renderer.GenerateMipmaps(this, rightRectangle, leftRectangle.Value);
            else
                Renderer.GenerateMipmaps(this, rightRectangle);

            // Uncomment the following block of code in order to compare the above with the OpenGL
            // reference mipmap generation GL.GenerateMipmap().
            // if (!manualMipmaps && uploadedRegions.Count != 0)
            // {
            //     GL.Hint(HintTarget.GenerateMipmapHint, HintMode.Nicest);
            //     GL.GenerateMipmap(TextureTarget.Texture2D);
            // }

            return true;
        }

        public bool UploadComplete
        {
            get
            {
                lock (uploadQueue)
                    return uploadQueue.Count == 0;
            }
        }

        /// <summary>
        /// Whether the texture is currently queued for upload.
        /// </summary>
        public bool IsQueuedForUpload { get; set; }

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
            if (textureId <= 0 || internalWidth != Width || internalHeight != Height)
            {
                internalWidth = Width;
                internalHeight = Height;

                // We only need to generate a new texture if we don't have one already. Otherwise just re-use the current one.
                if (textureId <= 0)
                {
                    int[] textures = new int[1];
                    GL.GenTextures(1, textures);

                    textureId = textures[0];

                    Renderer.BindTexture(this);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, IRenderer.MAX_MIPMAP_LEVELS);

                    // These shouldn't be required, but on some older Intel drivers the MAX_LOD chosen by the shader isn't clamped to the MAX_LEVEL from above, resulting in disappearing textures.
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinLod, 0);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLod, IRenderer.MAX_MIPMAP_LEVELS);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                        (int)(manualMipmaps ? filteringMode : filteringMode == All.Linear ? All.LinearMipmapLinear : All.Nearest));
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filteringMode);

                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                }
                else
                    Renderer.BindTexture(this);

                if ((Width == upload.Bounds.Width && Height == upload.Bounds.Height) || dataPointer == IntPtr.Zero)
                {
                    updateMemoryUsage(upload.Level, (long)Width * Height * 4);
                    GL.TexImage2D(TextureTarget2d.Texture2D, upload.Level, TextureComponentCount.Rgba8, Width, Height, 0, upload.Format, PixelType.UnsignedByte, dataPointer);
                }
                else
                {
                    initializeLevel(upload.Level, Width, Height, upload.Format);
                    GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Format,
                        PixelType.UnsignedByte, dataPointer);
                }

                if (!manualMipmaps)
                {
                    int width = internalWidth;
                    int height = internalHeight;

                    for (int level = 1; level < IRenderer.MAX_MIPMAP_LEVELS + 1 && (width > 1 || height > 1); ++level)
                    {
                        width = Math.Max(width >> 1, 1);
                        height = Math.Max(height >> 1, 1);

                        initializeLevel(level, width, height, upload.Format);
                    }
                }
            }

            // Just update content of the current texture
            else if (dataPointer != IntPtr.Zero)
            {
                Renderer.BindTexture(this);
                GL.TexSubImage2D(TextureTarget2d.Texture2D, upload.Level, upload.Bounds.X, upload.Bounds.Y, upload.Bounds.Width, upload.Bounds.Height, upload.Format, PixelType.UnsignedByte,
                    dataPointer);
            }
        }

        private void initializeLevel(int level, int width, int height, PixelFormat format)
        {
            updateMemoryUsage(level, (long)width * height * 4);
            GL.TexImage2D(TextureTarget2d.Texture2D, level, TextureComponentCount.Rgba8, width, height, 0, format, PixelType.UnsignedByte, IntPtr.Zero);

            // Initialize texture to solid color
            using var frameBuffer = new GLFrameBuffer(Renderer, this, level);
            Renderer.BindFrameBuffer(frameBuffer);
            Renderer.Clear(new ClearInfo(initialisationColour));
            Renderer.UnbindFrameBuffer(frameBuffer);
        }
    }
}
