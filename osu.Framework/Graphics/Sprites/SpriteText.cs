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

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteText : FlowContainer
    {
        /// <summary>
        /// The amount by which characters should overlap each other (negative character spacing).
        /// </summary>
        public float SpacingOverlap
        {
            get { return Spacing.X; }
            set
            {
                Spacing = new Vector2(value, 0);
                internalSize.Invalidate();
            }
        }

        public override bool IsPresent => base.IsPresent && !string.IsNullOrEmpty(text);

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

        public SpriteText(TextureStore store = null)
        {
            this.store = store;
            AutoSizeAxes = Axes.Both;
        }

        const float default_text_size = 20;

        private float textSize = default_text_size;

        public float TextSize
        {
            get
            {
                return textSize;
            }
            set
            {
                if (textSize == value) return;

                textSize = value;

                foreach (Drawable d in Children)
                    d.Scale = new Vector2(textSize);
            }
        }

        [BackgroundDependencyLoader]
        private void load(FontStore fonts)
        {
            if (store == null)
                store = fonts;

            spaceWidth = CreateCharacterDrawable('.')?.DrawWidth * 2 ?? default_text_size;

            if (!string.IsNullOrEmpty(text))
            {
                //this is used to prepare the initial string (useful for intial preloading).
                foreach (char c in text)
                    if (!char.IsWhiteSpace(c)) CreateCharacterDrawable(c);
            }
        }

        private string text;

        public string Text
        {
            get { return text; }
            set
            {
                if (text == value)
                    return;

                text = value;
                internalSize.Invalidate();
            }
        }

        private float? constantWidth;
        public bool FixedWidth;

        protected override void Update()
        {
            base.Update();
            refreshLayout();
        }

        string lastText;

        private void refreshLayout()
        {
            if (internalSize.EnsureValid()) return;

            internalSize.Refresh(delegate
            {
                if (FixedWidth && !constantWidth.HasValue)
                    constantWidth = CreateCharacterDrawable('D').DrawWidth;

                //keep sprites which haven't changed since last layout.
                List<Drawable> keepDrawables = new List<Drawable>();
                int length = Math.Min(lastText?.Length ?? 0, text?.Length ?? 0);

                keepDrawables.AddRange(Children.TakeWhile((n, i) => i < length && lastText[i] == text[i]));
                Remove(keepDrawables);
                Clear();

                foreach (var k in keepDrawables)
                    Add(k);

                for (int index = keepDrawables.Count; index < text.Length; index++)
                {
                    char c = text[index];

                    Drawable d;

                    if (char.IsWhiteSpace(c))
                    {
                        float width = FixedWidth ? constantWidth.GetValueOrDefault() : spaceWidth;

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

                        if (FixedWidth)
                        {
                            d.Anchor = Anchor.TopCentre;
                            d.Origin = Anchor.TopCentre;
                        }

                        var ctn = new Container
                        {
                            Size = new Vector2(FixedWidth ? constantWidth.GetValueOrDefault() : d.DrawSize.X, 1f),
                            Scale = new Vector2(TextSize),
                            Children = new[] { d }
                        };

                        if (shadow)
                        {
                            Drawable shadowDrawable = CreateCharacterDrawable(c);
                            shadowDrawable.Position = new Vector2(0, 0.06f);
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
