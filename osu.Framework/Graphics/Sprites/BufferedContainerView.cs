// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A view that displays the contents of a <see cref="BufferedContainer{T}"/>.
    /// </summary>
    public class BufferedContainerView<T> : Drawable, ITexturedShaderDrawable
        where T : Drawable
    {
        public IShader TextureShader { get; private set; }
        public IShader RoundedTextureShader { get; private set; }

        private BufferedContainer<T> container;
        private BufferedDrawNodeSharedData sharedData;

        internal BufferedContainerView(BufferedContainer<T> container, BufferedDrawNodeSharedData sharedData)
        {
            this.container = container;
            this.sharedData = sharedData;

            container.OnDispose += removeContainer;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            TextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            RoundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
        }

        protected override DrawNode CreateDrawNode() => new BufferSpriteDrawNode(this);

        private bool synchronisedDrawQuad;

        /// <summary>
        /// Whether this <see cref="BufferedContainerView{T}"/> should be drawn using the original <see cref="BufferedContainer{T}"/>'s draw quad.
        /// </summary>
        /// <remarks>
        /// This can be useful to display the <see cref="BufferedContainer{T}"/> as an overlay on top of itself.
        /// </remarks>
        public bool SynchronisedDrawQuad
        {
            get => synchronisedDrawQuad;
            set
            {
                if (value == synchronisedDrawQuad)
                    return;

                synchronisedDrawQuad = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private void removeContainer()
        {
            if (container == null)
                return;

            container.OnDispose -= removeContainer;

            container = null;
            sharedData = null;

            Invalidate(Invalidation.DrawNode);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            removeContainer();
        }

        private class BufferSpriteDrawNode : TexturedShaderDrawNode
        {
            protected new BufferedContainerView<T> Source => (BufferedContainerView<T>)base.Source;

            private Quad screenSpaceDrawQuad;
            private BufferedDrawNodeSharedData shared;

            public BufferSpriteDrawNode(BufferedContainerView<T> source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                screenSpaceDrawQuad = Source.synchronisedDrawQuad ? (Source.container?.ScreenSpaceDrawQuad ?? Source.ScreenSpaceDrawQuad) : Source.ScreenSpaceDrawQuad;
                wrapTexture = Source.WrapTexture;
                shared = Source.sharedData;
            }

            public override void Draw(Action<TexturedVertex2D> vertexAction)
            {
                base.Draw(vertexAction);

                if (shared?.MainBuffer?.Texture?.Available != true || shared.DrawVersion == -1)
                    return;

                Shader.Bind();
                DrawFrameBuffer(shared.MainBuffer, screenSpaceDrawQuad, DrawColourInfo.Colour, vertexAction);
                Shader.Unbind();
            }
        }
    }
}
