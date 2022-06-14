// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osu.Framework.Layout;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A sprite representing an icon.
    /// Uses <see cref="FontStore"/> to perform character lookups.
    /// </summary>
    public class SpriteIcon : CompositeDrawable
    {
        private Sprite spriteShadow;
        private Sprite spriteMain;

        private readonly LayoutValue layout = new LayoutValue(Invalidation.Colour, conditions: (s, _) => ((SpriteIcon)s).Shadow);
        private Container shadowVisibility;

        private FontStore store;

        public SpriteIcon()
        {
            AddLayout(layout);
        }

        [BackgroundDependencyLoader]
        private void load(FontStore store)
        {
            this.store = store;

            InternalChildren = new Drawable[]
            {
                shadowVisibility = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Child = spriteShadow = new Sprite
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fit,
                        Position = shadowOffset,
                        Colour = shadowColour
                    },
                    Alpha = shadow ? 1 : 0,
                },
                spriteMain = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit
                },
            };

            updateTexture();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateTexture();
            updateShadow();
        }

        private IconUsage loadedIcon;

        private void updateTexture()
        {
            var loadableIcon = icon;

            if (loadableIcon.Equals(loadedIcon)) return;

            var glyph = store.Get(loadableIcon.FontName, Icon.Icon);

            if (glyph != null)
            {
                spriteMain.Texture = glyph.Texture;
                spriteShadow.Texture = glyph.Texture;

                if (Size == Vector2.Zero)
                    Size = new Vector2(glyph.Width, glyph.Height);
            }

            loadedIcon = loadableIcon;
        }

        protected override void Update()
        {
            if (!layout.IsValid)
            {
                //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                //squared result for quadratic fall-off seems to give the best result.
                var avgColour = (Color4)DrawColourInfo.Colour.AverageColour;

                spriteShadow.Alpha = MathF.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                layout.Validate();
            }
        }

        private bool shadow;

        public bool Shadow
        {
            get => shadow;
            set
            {
                shadow = value;

                if (IsLoaded)
                    updateShadow();
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
                shadowColour = value;

                if (IsLoaded)
                    updateShadow();
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
                shadowOffset = value;

                if (IsLoaded)
                    updateShadow();
            }
        }

        private void updateShadow()
        {
            shadowVisibility.Alpha = shadow ? 1 : 0;
            spriteShadow.Colour = shadowColour;
            spriteShadow.Position = shadowOffset;
        }

        private IconUsage icon;

        public IconUsage Icon
        {
            get => icon;
            set
            {
                if (icon.Equals(value)) return;

                icon = value;
                if (IsLoaded)
                    updateTexture();
            }
        }
    }
}
