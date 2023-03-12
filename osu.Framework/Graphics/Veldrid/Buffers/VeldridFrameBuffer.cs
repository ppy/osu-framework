// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Textures;
using osuTK;
using Veldrid;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridFrameBuffer : IFrameBuffer
    {
        private readonly VeldridRenderer renderer;
        private readonly PixelFormat[] formats;
        private readonly VeldridTexture veldridTexture;

        public Texture Texture { get; }

        public Framebuffer Framebuffer { get; private set; }

        private Vector2 size = Vector2.One;

        public Vector2 Size
        {
            get => size;
            set
            {
                if (value == size)
                    return;

                size = value;

                veldridTexture.Width = (int)Math.Ceiling(size.X);
                veldridTexture.Height = (int)Math.Ceiling(size.Y);
                veldridTexture.SetData(new TextureUpload());
                veldridTexture.Upload();

                initialiseFramebuffer();
            }
        }

        public VeldridFrameBuffer(VeldridRenderer renderer, PixelFormat[]? formats = null, SamplerFilter filteringMode = SamplerFilter.MinLinear_MagLinear_MipLinear)
        {
            this.renderer = renderer;
            this.formats = formats ?? Array.Empty<PixelFormat>();

            Texture = renderer.CreateFrameBufferTexture(veldridTexture = new FrameBufferTexture(renderer, filteringMode));

            initialiseFramebuffer();
            Debug.Assert(Framebuffer != null);
        }

        private void initialiseFramebuffer()
        {
            VeldridTextureResources resources = veldridTexture.GetResourceList().Single();

            // todo: we probably want the arguments separated to "PixelFormat[] colorFormats, PixelFormat depthFormat".
            if (formats.Length > 1)
                throw new ArgumentException("Veldrid framebuffer cannot contain more than one depth target.");

            FramebufferDescription description = new FramebufferDescription
            {
                ColorTargets = new[]
                {
                    new FramebufferAttachmentDescription(resources.Texture, 0)
                }
            };

            if (formats.Length > 0)
            {
                TextureDescription depthDescription = TextureDescription.Texture2D((uint)veldridTexture.Width, (uint)veldridTexture.Height, 1, 1, formats[0], TextureUsage.DepthStencil);
                description.DepthTarget = new FramebufferAttachmentDescription(renderer.Factory.CreateTexture(ref depthDescription), 0);
            }

            Framebuffer = renderer.Factory.CreateFramebuffer(ref description);
        }

        public void Bind() => renderer.BindFrameBuffer(this);
        public void Unbind() => renderer.UnbindFrameBuffer(this);

        ~VeldridFrameBuffer()
        {
            renderer.ScheduleDisposal(b => b.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            veldridTexture.Dispose();

            renderer.DeleteFrameBuffer(this);

            isDisposed = true;
        }

        private class FrameBufferTexture : VeldridTexture
        {
            protected override TextureUsage Usages => base.Usages | TextureUsage.RenderTarget;

            public FrameBufferTexture(VeldridRenderer renderer, SamplerFilter filteringMode = SamplerFilter.MinLinear_MagLinear_MipLinear)
                : base(renderer, 1, 1, true, filteringMode)
            {
                BypassTextureUploadQueueing = true;

                SetData(new TextureUpload());
                Upload();
            }

            public override int Width
            {
                get => base.Width;
                set => base.Width = Math.Clamp(value, 1, Renderer.MaxTextureSize);
            }

            public override int Height
            {
                get => base.Height;
                set => base.Height = Math.Clamp(value, 1, Renderer.MaxTextureSize);
            }
        }
    }
}
