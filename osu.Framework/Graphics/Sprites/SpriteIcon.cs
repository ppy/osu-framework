// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A drawable representing an icon.
    /// Uses <see cref="FontStore"/> to perform character lookups.
    /// </summary>
    public partial class SpriteIcon : Drawable, ITexturedShaderDrawable
    {
        public IShader? TextureShader { get; private set; }

        private FontStore store = null!;

        [BackgroundDependencyLoader]
        private void load(FontStore store, ShaderManager shaders)
        {
            this.store = store;
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            updateTexture();
        }

        private IconUsage loadedIcon;
        private Texture? texture;

        private void updateTexture()
        {
            var loadableIcon = icon;

            if (loadableIcon.Equals(loadedIcon)) return;

            var glyph = store.Get(loadableIcon.FontName, Icon.Icon);

            if (glyph != null)
            {
                texture = glyph.Texture;

                if (Size == Vector2.Zero)
                    Size = new Vector2(glyph.Width, glyph.Height);
            }

            loadedIcon = loadableIcon;
            Invalidate(Invalidation.DrawNode);
        }

        private bool shadow;

        public bool Shadow
        {
            get => shadow;
            set
            {
                if (shadow == value)
                    return;

                shadow = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4 shadowColour = new Color4(0f, 0f, 0f, 0.2f);

        /// <summary>
        /// The colour of the shadow displayed around the icon. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Color4 ShadowColour
        {
            get => shadowColour;
            set
            {
                if (shadowColour == value)
                    return;

                shadowColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Vector2 shadowOffset = new Vector2(0, 2f);

        /// <summary>
        /// The offset of the shadow displayed around the icon. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Vector2 ShadowOffset
        {
            get => shadowOffset;
            set
            {
                if (shadowOffset == value)
                    return;

                shadowOffset = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private IconUsage icon;

        public IconUsage Icon
        {
            get => icon;
            set
            {
                if (icon.Equals(value)) return;

                icon = value;
                if (LoadState > LoadState.NotLoaded)
                    updateTexture();
            }
        }

        protected override DrawNode CreateDrawNode() => new SpriteIconDrawNode(this);

        private class SpriteIconDrawNode : TexturedShaderDrawNode
        {
            protected new SpriteIcon Source => (SpriteIcon)base.Source;

            public SpriteIconDrawNode(SpriteIcon source)
                : base(source)
            {
            }

            private bool shadow;
            private ColourInfo shadowDrawColour;
            private Quad shadowDrawQuad;
            private Quad screenSpaceDrawQuad;
            private Texture? texture;

            public override void ApplyState()
            {
                base.ApplyState();

                texture = Source.texture;
                if (texture == null)
                    return;

                shadow = Source.shadow;

                RectangleF drawRect = Source.DrawRectangle;

                // scale texture to fit into drawable
                float scale = Math.Min(drawRect.Width / texture.Width, drawRect.Height / texture.Height);
                drawRect.Size = texture.Size * scale;

                // move draw rectangle to make texture centered
                drawRect.Location += (Source.DrawRectangle.Size - drawRect.Size) * 0.5f;
                screenSpaceDrawQuad = Source.ToScreenSpace(drawRect);

                if (!shadow)
                    return;

                RectangleF offsetRect = drawRect;
                offsetRect.Location += Source.shadowOffset;
                shadowDrawQuad = Source.ToScreenSpace(offsetRect);

                ColourInfo shadowCol = Source.shadowColour;

                //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                //squared result for quadratic fall-off seems to give the best result.
                var avgColour = (Color4)DrawColourInfo.Colour.AverageColour;
                float alpha = MathF.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                shadowCol = shadowCol.MultiplyAlpha(alpha);

                shadowDrawColour = DrawColourInfo.Colour;
                shadowDrawColour.ApplyChild(shadowCol);
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (texture?.Available != true)
                    return;

                BindTextureShader(renderer);

                if (shadow)
                    renderer.DrawQuad(texture, shadowDrawQuad, shadowDrawColour);

                renderer.DrawQuad(texture, screenSpaceDrawQuad, DrawColourInfo.Colour);

                UnbindTextureShader(renderer);
            }
        }
    }
}
