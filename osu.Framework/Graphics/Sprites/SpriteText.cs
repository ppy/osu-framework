// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.IO.Stores;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteText : FillFlowContainer, IHasCurrentValue<string>
    {
        private static readonly char[] default_fixed_width_exceptions = { '.', ':', ',' };

        /// <summary>
        /// An array of characters which should not get a fixed width in a <see cref="FixedWidth"/> instance.
        /// </summary>
        protected virtual char[] FixedWidthExceptionCharacters => default_fixed_width_exceptions;

        /// <summary>
        /// Decide whether we want to make our SpriteText's vertical size to be <see cref="TextSize"/> (the full height) or precisely the size of used characters.
        /// Set to false to allow better centering of individual characters/numerals/etc.
        /// </summary>
        public bool UseFullGlyphHeight = true;

        public override bool IsPresent => base.IsPresent && !string.IsNullOrEmpty(text);

        public bool AllowMultiline
        {
            get { return Direction == FillDirection.Full; }
            set { Direction = value ? FillDirection.Full : FillDirection.Horizontal; }
        }

        private string font;

        public string Font
        {
            get { return font; }
            set
            {
                font = value;
                internalSize.Invalidate();
            }
        }

        private bool shadow;

        public bool Shadow
        {
            get { return shadow; }
            set
            {
                if (shadow == value) return;

                shadow = value;
                internalSize.Invalidate(); // Trigger a layout refresh
            }
        }


        private Color4 shadowColour = new Color4(0f, 0f, 0f, 0.2f);

        public Color4 ShadowColour
        {
            get { return shadowColour; }
            set
            {
                shadowColour = value;
                if (shadow)
                    internalSize.Invalidate();
            }
        }

        private Cached<Vector2> internalSize = new Cached<Vector2>();

        private float spaceWidth;

        private TextureStore store;

        public override bool HandleInput => false;

        public SpriteText()
        {
            AutoSizeAxes = Axes.Both;
        }

        private const float default_text_size = 20;

        private float textSize = default_text_size;

        public float TextSize
        {
            get { return textSize; }
            set
            {
                if (textSize == value) return;

                textSize = value;

                foreach (Drawable d in Children)
                    d.Scale = new Vector2(textSize);
            }
        }

        [BackgroundDependencyLoader]
        private void load(FontStore store)
        {
            this.store = store;

            spaceWidth = CreateCharacterDrawable('.')?.DrawWidth * 2 ?? default_text_size;

            if (!string.IsNullOrEmpty(text))
            {
                //this is used to prepare the initial string (useful for intial preloading).
                foreach (char c in text)
                    if (!char.IsWhiteSpace(c)) CreateCharacterDrawable(c);
            }
        }

        private Bindable<string> current;

        public Bindable<string> Current
        {
            get { return current; }
            set
            {
                if (current != null)
                    current.ValueChanged -= setText;
                if (value != null)
                {
                    value.ValueChanged += setText;
                    value.TriggerChange();
                }

                current = value;
            }
        }

        private void setText(string newText)
        {
            if (text == newText)
                return;

            text = newText ?? string.Empty;
            internalSize.Invalidate();
        }

        private string text = string.Empty;

        public string Text
        {
            get { return text; }
            set
            {
                if (current != null)
                    throw new InvalidOperationException($@"property {nameof(Text)} cannot be set manually if {nameof(Current)} set");

                setText(value);
            }
        }

        private float? constantWidth;
        public bool FixedWidth;

        protected override void Update()
        {
            base.Update();
            refreshLayout();
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.Colour) > 0 && Shadow)
                internalSize.Invalidate(); //we may need to recompute the shadow alpha if our text colour has changed (see shadowAlpha).

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        private string lastText;
        private float lastShadowAlpha;
        private string lastFont;

        private void refreshLayout()
        {
            if (internalSize.EnsureValid()) return;

            internalSize.Refresh(delegate
            {
                if (FixedWidth && !constantWidth.HasValue)
                    constantWidth = CreateCharacterDrawable('D').DrawWidth;

                //keep sprites which haven't changed since last layout.
                List<Drawable> keepDrawables = new List<Drawable>();

                bool allowKeepingExistingDrawables = true;

                //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
                //squared result for quadratic fall-off seems to give the best result.
                var avgColour = (Color4)DrawInfo.Colour.AverageColour;
                float shadowAlpha = (float)Math.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

                //we can't keep existing drawabled if our shadow has changed, as the shadow is applied in the add-loop.
                //this could potentially be optimised if necessary.
                allowKeepingExistingDrawables &= shadowAlpha == lastShadowAlpha && font == lastFont;

                lastShadowAlpha = shadowAlpha;
                lastFont = font;

                if (allowKeepingExistingDrawables)
                {
                    int length = Math.Min(lastText?.Length ?? 0, text.Length);
                    keepDrawables.AddRange(Children.TakeWhile((n, i) => i < length && lastText[i] == text[i]));
                    Remove(keepDrawables); //doesn't dispose
                }

                Clear();

                if (text.Length == 0) return Vector2.Zero;

                foreach (var k in keepDrawables)
                    Add(k);

                for (int index = keepDrawables.Count; index < text.Length; index++)
                {
                    char c = text[index];

                    bool fixedWidth = FixedWidth && !FixedWidthExceptionCharacters.Contains(c);

                    Drawable d;

                    if (char.IsWhiteSpace(c))
                    {
                        float width = fixedWidth ? constantWidth.GetValueOrDefault() : spaceWidth;

                        switch ((int)c)
                        {
                            case 0x3000: //double-width space
                                width *= 2;
                                break;
                        }

                        d = new Container
                        {
                            Size = new Vector2(width),
                            Scale = new Vector2(TextSize),
                            Colour = Color4.Transparent,
                        };
                    }
                    else
                    {
                        d = CreateCharacterDrawable(c);

                        if (fixedWidth)
                        {
                            d.Anchor = Anchor.TopCentre;
                            d.Origin = Anchor.TopCentre;
                        }

                        var ctn = new Container
                        {
                            Size = new Vector2(fixedWidth ? constantWidth.GetValueOrDefault() : d.DrawSize.X, UseFullGlyphHeight ? 1 : d.DrawSize.Y),
                            Scale = new Vector2(TextSize),
                            Children = new[] { d }
                        };

                        if (shadow && shadowAlpha > 0)
                        {
                            Drawable shadowDrawable = CreateCharacterDrawable(c);
                            shadowDrawable.Position = new Vector2(0, 0.06f);
                            shadowDrawable.Anchor = d.Anchor;
                            shadowDrawable.Origin = d.Origin;
                            shadowDrawable.Alpha = shadowAlpha;
                            shadowDrawable.Colour = shadowColour;
                            shadowDrawable.Depth = float.MaxValue;
                            ctn.Add(shadowDrawable);
                        }

                        d = ctn;
                    }

                    Add(d);
                }

                lastText = text;
                return Vector2.Zero;
            });
        }

        protected virtual Drawable CreateFallbackCharacterDrawable() => new Box
        {
            Origin = Anchor.Centre,
            Anchor = Anchor.Centre,
            Scale = new Vector2(0.7f)
        };

        protected virtual Drawable CreateCharacterDrawable(char c)
        {
            var tex = GetTextureForCharacter(c);
            if (tex != null)
                return new Sprite { Texture = tex };

            return CreateFallbackCharacterDrawable();
        }

        protected Texture GetTextureForCharacter(char c)
        {
            return store?.Get(getTextureName(c)) ?? store?.Get(getTextureName(c, false));
        }

        private string getTextureName(char c, bool useFont = true) => !useFont || string.IsNullOrEmpty(Font) ? c.ToString() : $@"{Font}/{c}";

        public override string ToString()
        {
            return $@"""{Text}"" " + base.ToString();
        }
    }
}
