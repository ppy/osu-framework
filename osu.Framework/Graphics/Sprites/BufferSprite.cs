// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A sprite that displays the contents of a <see cref="BufferedContainer{T}"/>.
    /// </summary>
    public class BufferSprite : Drawable
    {
        private Shader textureShader;
        private Shader roundedTextureShader;

        /// <summary>
        /// True if the texture should be tiled. If you had a 16x16 texture and scaled the sprite to be 64x64 the texture would be repeated in a 4x4 grid along the size of the sprite.
        /// </summary>
        public bool WrapTexture;

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            texture?.Dispose();
            texture = null;

            base.Dispose(isDisposing);
        }

        #endregion

        private readonly WeakReference<BufferedContainer<Drawable>> buffered;

        public BufferSprite(BufferedContainer<Drawable> buffered)
        {
            this.buffered = new WeakReference<BufferedContainer<Drawable>>(buffered);
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            textureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
        }

        protected override DrawNode CreateDrawNode() => new BufferSpriteDrawNode();

        public BufferedContainer<Drawable> BufferedContainer => buffered.TryGetTarget(out var b) ? b : null;

        private Texture texture;
        public Texture Texture
        {
            get
            {
                if (BufferedContainer == null)
                    return null;

                if (texture != null)
                    return texture;

                var frame = BufferedContainer.sharedData.FrameBuffers[0];

                if (frame.IsInitialized)
                    return texture = new Texture(frame.Texture);

                return null;
            }
        }

        private bool syncDrawQuad;
        /// <summary>
        /// Whether we should inherit the draw quad of the target <see cref="BufferedContainer{T}"/>.
        /// </summary>
        /// <remarks>
        /// This property can be useful when setting up a "frosting" effect where the
        /// background framebuffer's contents are rendered again in the overlay's framebuffer.
        /// </remarks>
        public bool SynchronizedDrawQuad
        {
            get => syncDrawQuad;
            set
            {
                if (value == syncDrawQuad)
                    return;

                syncDrawQuad = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        protected override void ApplyDrawNode(DrawNode node)
        {
            BufferSpriteDrawNode n = (BufferSpriteDrawNode)node;

            n.ScreenSpaceDrawQuad = syncDrawQuad ? BufferedContainer?.ScreenSpaceDrawQuad ?? ScreenSpaceDrawQuad : ScreenSpaceDrawQuad;
            n.DrawRectangle = syncDrawQuad ? BufferedContainer?.DrawRectangle ?? DrawRectangle : DrawRectangle;
            n.WrapTexture = WrapTexture;
            n.Shared = BufferedContainer?.sharedData;

            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;

            base.ApplyDrawNode(node);
        }

        public class BufferSpriteDrawNode : DrawNode
        {
            public Quad ScreenSpaceDrawQuad;
            public RectangleF DrawRectangle;
            public bool WrapTexture;
            public BufferedContainerDrawNodeSharedData Shared;

            public Shader TextureShader;
            public Shader RoundedTextureShader;

            private bool needsRoundedShader => GLWrapper.IsMaskingActive;

            private TextureGL getCurrentFrameBufferTexture()
            {
                if (Shared == null || Shared.LastFrameBufferIndex == -1)
                    return null;

                var index = Shared.LastFrameBufferIndex;

                if (Shared.FrameBuffers[index] != null)
                    return Shared.FrameBuffers[index].Texture;

                return null;
            }

            protected virtual void Blit(TextureGL tex, Action<TexturedVertex2D> vertexAction)
            {
                // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
                RectangleF textureRect = new RectangleF(0, tex.Height, tex.Width, -tex.Height);

                tex.DrawQuad(ScreenSpaceDrawQuad, textureRect, DrawColourInfo.Colour, vertexAction, Vector2.Zero);
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                var tex = getCurrentFrameBufferTexture();

                if (tex?.IsDisposed == true)
                    return;

                Shader shader = needsRoundedShader ? RoundedTextureShader : TextureShader;

                shader.Bind();

                tex.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

                Blit(tex, vertexAction);

                shader.Unbind();
            }
        }
    }
}