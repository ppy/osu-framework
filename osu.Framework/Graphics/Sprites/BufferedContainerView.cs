// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;

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

        private bool displayOriginalEffects;

        /// <summary>
        /// Whether the effects drawn by the <see cref="BufferedContainer{T}"/> should also be drawn for this view.
        /// </summary>
        public bool DisplayOriginalEffects
        {
            get => displayOriginalEffects;
            set
            {
                if (displayOriginalEffects == value)
                    return;

                displayOriginalEffects = value;

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
            private bool displayOriginalEffects;

            private bool sourceDrawsOriginal;
            private ColourInfo sourceEffectColour;
            private BlendingParameters sourceEffectBlending;
            private EffectPlacement sourceEffectPlacement;

            public BufferSpriteDrawNode(BufferedContainerView<T> source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                screenSpaceDrawQuad = Source.synchronisedDrawQuad ? Source.container.ScreenSpaceDrawQuad : Source.ScreenSpaceDrawQuad;
                shared = Source.sharedData;

                displayOriginalEffects = Source.displayOriginalEffects;
                sourceDrawsOriginal = Source.container.DrawOriginal;
                sourceEffectColour = Source.container.EffectColour;
                sourceEffectBlending = Source.container.DrawEffectBlending;
                sourceEffectPlacement = Source.container.EffectPlacement;
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (shared?.MainBuffer?.Texture.Available != true || shared.DrawVersion == -1)
                    return;

                var shader = GetAppropriateShader(renderer);

                shader.Bind();

                if (sourceEffectPlacement == EffectPlacement.InFront)
                    drawMainBuffer(renderer);

                drawEffectBuffer(renderer);

                if (sourceEffectPlacement == EffectPlacement.Behind)
                    drawMainBuffer(renderer);

                shader.Unbind();
            }

            private void drawMainBuffer(IRenderer renderer)
            {
                // If the original was drawn, draw it.
                // Otherwise, if an effect will also not be drawn then we still need to display something - the original.
                // Keep in mind that the effect MAY be the original itself, but is drawn through drawEffectBuffer().
                if (!sourceDrawsOriginal && shouldDrawEffectBuffer)
                    return;

                renderer.SetBlend(DrawColourInfo.Blending);
                renderer.DrawFrameBuffer(shared.MainBuffer, screenSpaceDrawQuad, DrawColourInfo.Colour);
            }

            private void drawEffectBuffer(IRenderer renderer)
            {
                if (!shouldDrawEffectBuffer)
                    return;

                renderer.SetBlend(sourceEffectBlending);
                ColourInfo finalEffectColour = DrawColourInfo.Colour;
                finalEffectColour.ApplyChild(sourceEffectColour);

                renderer.DrawFrameBuffer(shared.CurrentEffectBuffer, screenSpaceDrawQuad, DrawColourInfo.Colour);
            }

            /// <summary>
            /// Whether the source's current effect buffer should be drawn.
            /// This is true if we explicitly want to draw it or if no effects were drawn by the source. In the case that no effects were drawn by the source,
            /// the current effect buffer will be the main buffer, and what will be drawn is the main buffer with the effect blending applied.
            /// </summary>
            private bool shouldDrawEffectBuffer => displayOriginalEffects || shared.CurrentEffectBuffer == shared.MainBuffer;
        }
    }
}
