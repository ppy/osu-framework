// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Cached;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteText : FlowContainer
    {
        /// <summary>
        /// The amount by which characters should overlap each other (negative character spacing).
        /// </summary>
        public float SpacingOverlap
        {
            get { return Padding.X; }
            set { Padding = new Vector2(value, 0); }
        }

        public override bool IsVisible => base.IsVisible && !string.IsNullOrEmpty(text);

        private Cached<Vector2> internalSize = new Cached<Vector2>();

        private float spaceWidth;

        private TextureStore store;

        public override bool HandleInput => false;

        public SpriteText(TextureStore store = null)
        {
            this.store = store;
        }

        internal override Vector2 ChildScale => new Vector2(TextSize);

        private float textSize = 20;

        public float TextSize
        {
            get { return textSize; }
            set
            {
                if (textSize == value) return;

                textSize = value;
                Invalidate(Invalidation.Position);
            }
        }

        public override void Load()
        {
            base.Load();

            if (store == null)
                store = Game.Fonts;

            spaceWidth = getSprite('.')?.Width * 2 ?? 20;
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

        public override Vector2 Size
        {
            get
            {
                if (constantWidth.HasValue && !HasDefinedSize)
                    // We can determine the size even in the case autosize hasn't been run here, because we override autosize
                    refreshLayout();

                return base.Size;
            }
        }

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
                    constantWidth = getSprite('D').Width;

                //keep sprites which haven't changed since last layout.
                List<Drawable> keepDrawables = new List<Drawable>();
                int length = Math.Min(lastText?.Length ?? 0, text?.Length ?? 0);

                keepDrawables.AddRange(Children.TakeWhile((n, i) => i < length && lastText[i] == text[i]));
                Remove(keepDrawables, false);
                Clear();

                foreach (var k in keepDrawables)
                    Add(k);

                for (int index = keepDrawables.Count; index < text.Length; index++)
                {
                    char c = text[index];

                    Drawable s;

                    if (char.IsWhiteSpace(c))
                    {
                        float width = FixedWidth ? constantWidth.Value : spaceWidth;

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
                            Size = new Vector2(FixedWidth ? constantWidth.Value : s.Size.X, 1f),
                            Children = new[] { s }
                        };

                        s = ctn;
                    }

                    Add(s);
                }

                base.UpdateChildrenLife();

                lastText = text;
                return Vector2.Zero;
            });
        }

        private Sprite getSprite(char c) => new Sprite
        {
            Texture = getTexture(c)
        };

        private Texture getTexture(char c) => store?.Get(getTextureName(c));
        private string getTextureName(char c) => $@"{c}";

        public override string ToString()
        {
            return $@"""{Text}"" " + base.ToString();
        }

        protected override bool UpdateChildrenLife()
        {
            return false;
        }
    }
}
