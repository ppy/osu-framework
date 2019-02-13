// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Sprites
{
    public class BufferSprite : Drawable
    {
        private Shader textureShader;
        private Shader roundedTextureShader;

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
            n.Shared = BufferedContainer?.sharedData;

            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;

            base.ApplyDrawNode(node);
        }

        public class BufferSpriteDrawNode : SpriteDrawNode
        {
            public BufferedContainerDrawNodeSharedData Shared;

            private readonly Texture[] textures = new Texture[2];

            private Texture getCurrentFrameBufferTexture()
            {
                if (Shared == null || Shared.LastFrameBufferIndex == -1)
                    return null;

                var index = Shared.LastFrameBufferIndex;

                if (textures[index] != null)
                    return textures[index];

                var frame = Shared.FrameBuffers[index];

                if (frame.IsInitialized)
                    return textures[index] = new Texture(frame.Texture);

                return null;
            }

            protected override void Blit(Action<TexturedVertex2D> vertexAction)
            {
                // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
                RectangleF textureRect = new RectangleF(0, Texture.Height, Texture.Width, -Texture.Height);

                Texture.DrawQuad(ScreenSpaceDrawQuad, DrawColourInfo.Colour, textureRect, vertexAction,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                Texture = getCurrentFrameBufferTexture();

                base.Draw(vertexAction);
            }
        }
    }
}