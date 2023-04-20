// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Development;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Platform;
using osuTK.Graphics;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using PixelFormat = Veldrid.PixelFormat;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid.Textures
{
    internal class VeldridTexture : INativeTexture
    {
        private readonly Queue<ITextureUpload> uploadQueue = new Queue<ITextureUpload>();

        IRenderer INativeTexture.Renderer => Renderer;

        public string Identifier
        {
            get
            {
                if (!Available || resources == null)
                    return "-";

                return resources.Texture.Name;
            }
        }

        public int MaxSize => Renderer.MaxTextureSize;

        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public virtual int GetByteSize() => Width * Height * 4;
        public bool Available { get; private set; } = true;

        ulong INativeTexture.TotalBindCount { get; set; }

        public bool BypassTextureUploadQueueing { get; set; }

        private readonly bool manualMipmaps;

        private readonly SamplerFilter filteringMode;
        private readonly Color4 initialisationColour;

        public ulong BindCount { get; protected set; }

        public RectangleI Bounds => new RectangleI(0, 0, Width, Height);

        protected virtual TextureUsage Usages
        {
            get
            {
                var usages = TextureUsage.Sampled;

                if (!manualMipmaps && Renderer.Device.Features.ComputeShader)
                    usages |= TextureUsage.Storage;

                return usages;
            }
        }

        protected readonly VeldridRenderer Renderer;

        /// <summary>
        /// Creates a new <see cref="VeldridTexture"/>.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads).</param>
        public VeldridTexture(VeldridRenderer renderer, int width, int height, bool manualMipmaps = false, SamplerFilter filteringMode = SamplerFilter.MinLinear_MagLinear_MipLinear,
                              Color4 initialisationColour = default)
        {
            this.manualMipmaps = manualMipmaps;
            this.filteringMode = filteringMode;
            this.initialisationColour = initialisationColour;

            Renderer = renderer;
            Width = width;
            Height = height;
        }

        #region Disposal

        ~VeldridTexture()
        {
            Dispose(false);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            Renderer.ScheduleDisposal(texture =>
            {
                while (texture.tryGetNextUpload(out var upload))
                    upload.Dispose();

                texture.memoryLease?.Dispose();

                texture.resources?.Dispose();
                texture.resources = null;

                texture.Available = false;
            }, this);
        }

        #endregion

        #region Memory Tracking

        private readonly List<long> levelMemoryUsage = new List<long>();
        private NativeMemoryTracker.NativeMemoryLease? memoryLease;

        private void updateMemoryUsage(int level, long newUsage)
        {
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

        private readonly VeldridTextureResources?[] resourcesArray = new VeldridTextureResources?[1];

        private VeldridTextureResources? resources
        {
            get => resourcesArray[0];
            set => resourcesArray[0] = value;
        }

        public virtual IReadOnlyList<VeldridTextureResources> GetResourceList() => resourcesArray!;

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

        public virtual bool Bind(int unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (!Available)
                throw new ObjectDisposedException(ToString(), "Can not bind a disposed texture.");

            Upload();

            if (resources == null)
                return false;

            if (Renderer.BindTexture(this, wrapModeS: wrapModeS, wrapModeT: wrapModeT))
                BindCount++;

            return true;
        }

        public bool Upload()
        {
            if (!Available)
                return false;

            // We should never run raw Veldrid calls on another thread than the draw thread due to race conditions.
            ThreadSafety.EnsureDrawThread();

            List<RectangleI> uploadedRegions = new List<RectangleI>();

            while (tryGetNextUpload(out ITextureUpload? upload))
            {
                using (upload)
                {
                    DoUpload(upload);

                    uploadedRegions.Add(upload.Bounds);
                }
            }

            // Generate mipmaps for just the updated regions of the texture.
            // This implementation is functionally equivalent to CommandList.GenerateMipmaps(),
            // only that it is much more efficient if only small parts of the texture
            // have been updated.
            if (uploadedRegions.Count != 0 && !manualMipmaps)
            {
                // Merge overlapping upload regions to prevent redundant mipmap generation.
                // i goes through the list left-to-right, j goes through it right-to-left
                // until both indices meet somewhere in the middle.
                // This algorithm needs multiple passes until no possible merges are found.
                bool mergeFound;

                do
                {
                    mergeFound = false;

                    for (int i = 0; i < uploadedRegions.Count; ++i)
                    {
                        RectangleI toMerge = uploadedRegions[i];

                        for (int j = uploadedRegions.Count - 1; j > i; --j)
                        {
                            RectangleI mergeCandidate = uploadedRegions[j];

                            if (!toMerge.Intersect(mergeCandidate).IsEmpty)
                            {
                                uploadedRegions[i] = toMerge = RectangleI.Union(toMerge, mergeCandidate);
                                uploadedRegions.RemoveAt(j);
                                mergeFound = true;
                            }
                        }
                    }
                } while (mergeFound);

                Renderer.GenerateMipmaps(this, uploadedRegions);
            }

            // Uncomment the following block of code in order to compare the above with the renderer mipmap generation method CommandList.GenerateMipmaps().
            // if (uploadedRegions.Count != 0 && !manualMipmaps)
            // {
            //     Debug.Assert(resources != null);
            //     Renderer.Commands.GenerateMipmaps(resources.Texture);
            // }

            return uploadedRegions.Count != 0;
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

        private bool tryGetNextUpload([NotNullWhen(true)] out ITextureUpload? upload)
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

        private int? mipLevel;

        public int? MipLevel
        {
            get => mipLevel;
            set
            {
                if (mipLevel == value)
                    return;

                mipLevel = value;

                if (resources != null)
                    resources.Sampler = createSampler();
            }
        }

        /// <summary>
        /// The maximum number of mip levels provided by an <see cref="ITextureUpload"/>.
        /// </summary>
        private int maximumUploadedLod;

        private Sampler createSampler()
        {
            bool useUploadMipmaps = manualMipmaps || maximumUploadedLod > 0;
            int maximumLod = useUploadMipmaps ? maximumUploadedLod : IRenderer.MAX_MIPMAP_LEVELS;

            var samplerDescription = new SamplerDescription
            {
                AddressModeU = SamplerAddressMode.Clamp,
                AddressModeV = SamplerAddressMode.Clamp,
                AddressModeW = SamplerAddressMode.Clamp,
                Filter = filteringMode,
                MinimumLod = (uint)(MipLevel ?? 0),
                MaximumLod = (uint)(MipLevel ?? maximumLod),
                MaximumAnisotropy = 0,
            };

            return Renderer.Factory.CreateSampler(ref samplerDescription);
        }

        protected virtual void DoUpload(ITextureUpload upload)
        {
            Texture? texture = resources?.Texture;
            Sampler? sampler = resources?.Sampler;
            bool newTexture = false;

            if (texture == null || texture.Width != Width || texture.Height != Height)
            {
                texture?.Dispose();

                var textureDescription = TextureDescription.Texture2D((uint)Width, (uint)Height, (uint)CalculateMipmapLevels(Width, Height), 1, PixelFormat.R8_G8_B8_A8_UNorm, Usages);
                texture = Renderer.Factory.CreateTexture(ref textureDescription);
                newTexture = true;

                maximumUploadedLod = 0;
            }

            int lastMaximumUploadedLod = maximumUploadedLod;

            if (!upload.Data.IsEmpty && upload.Level > maximumUploadedLod)
                maximumUploadedLod = upload.Level;

            if (sampler == null || maximumUploadedLod > lastMaximumUploadedLod)
                sampler = createSampler();

            resources = new VeldridTextureResources(texture, sampler);

            if (newTexture)
            {
                for (int i = 0; i < texture.MipLevels; i++)
                    initialiseLevel(i, Width >> i, Height >> i);
            }

            if (!upload.Data.IsEmpty)
            {
                Renderer.UpdateTexture(texture, upload.Bounds.X >> upload.Level, upload.Bounds.Y >> upload.Level, upload.Bounds.Width >> upload.Level, upload.Bounds.Height >> upload.Level,
                    upload.Level, upload.Data);
            }
        }

        private unsafe void initialiseLevel(int level, int width, int height)
        {
            updateMemoryUsage(level, (long)width * height * sizeof(Rgba32));

            using var commands = Renderer.Factory.CreateCommandList();
            using var frameBuffer = new VeldridFrameBuffer(Renderer, this, level);

            commands.Begin();

            // Initialize texture to solid color
            commands.SetFramebuffer(frameBuffer.Framebuffer);
            commands.ClearColorTarget(0, new RgbaFloat(initialisationColour.R, initialisationColour.G, initialisationColour.B, initialisationColour.A));

            commands.End();
            Renderer.Device.SubmitCommands(commands);
        }

        // todo: should this be limited to MAX_MIPMAP_LEVELS or was that constant supposed to be for automatic mipmap generation only?
        // previous implementation was allocating mip levels all the way to 1x1 size when an ITextureUpload.Level > 0, therefore it's not limited there.
        protected static int CalculateMipmapLevels(int width, int height) => 1 + (int)Math.Floor(Math.Log(Math.Max(width, height), 2));
    }
}
