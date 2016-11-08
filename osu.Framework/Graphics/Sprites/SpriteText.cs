// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
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

        public override bool IsVisible => base.IsVisible && !string.IsNullOrEmpty(text);

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

        private Cached<Vector2> internalSize = new Cached<Vector2>();

        private float spaceWidth;

        private TextureStore store;

        public override bool HandleInput => false;

        public SpriteText(TextureStore store = null)
        {
            this.store = store;
            AutoSizeAxes = Axes.Both;
        }

        public override Vector2 ChildScale => new Vector2(TextSize);

        private float textSize = 20;

        public float TextSize
        {
            get { return textSize; }
            set
            {
                if (textSize == value) return;

                textSize = value;
                Invalidate(Invalidation.Geometry);
            }
        }

        [Initializer]
        private void Load(FontStore fonts)
        {
            if (store == null)
                store = fonts;

            spaceWidth = getSprite('.')?.DrawWidth * 2 ?? 20;

            if (!string.IsNullOrEmpty(text))
            {
                //this is used to prepare the initial string (useful for intial preloading).
                foreach (char c in text)
                    if (!char.IsWhiteSpace(c)) getSprite(c);
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
                    constantWidth = getSprite('D').DrawWidth;

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

                    Drawable s;

                    if (char.IsWhiteSpace(c))
                    {
                        float width = FixedWidth ? constantWidth.GetValueOrDefault() : spaceWidth;

                        switch ((int)c)
                        {
                            case 0x3000: //double-width space
                                width *= 2;
                                break;
                        }

                        s = new Container
                        {
                            Size = new Vector2(width),
                            Colour = Color4.Transparent,
                        };
                    }
                    else
                    {
                        s = getSprite(c);

                        if (FixedWidth)
                        {
                            s.Anchor = Anchor.TopCentre;
                            s.Origin = Anchor.TopCentre;
                        }

                        var ctn = new Container
                        {
                            Size = new Vector2(FixedWidth ? constantWidth.GetValueOrDefault() : s.DrawSize.X, 1f),
                            Children = new[] { s }
                        };

                        s = ctn;
                    }

                    Add(s);
                }

                lastText = text;
                return Vector2.Zero;
            });
        }

        private Sprite getSprite(char c) => new Sprite
        {
            Texture = getTexture(c)
        };

        private Texture getTexture(char c) => store?.Get(getTextureName(c));
        private string getTextureName(char c) => string.IsNullOrEmpty(Font) ? c.ToString() : $@"{Font}/{c}";

        public override string ToString()
        {
            return $@"""{Text}"" " + base.ToString();
        }
    }
}
