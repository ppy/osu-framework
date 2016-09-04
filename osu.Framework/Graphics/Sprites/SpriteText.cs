//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics.Textures;
using osu.Framework.Cached;
using osu.Framework.Graphics.Containers;
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
            get { return Padding.X; }
            set
            {
                Padding = new Vector2(value, 0);
            }
        }

        public override bool IsVisible => base.IsVisible && !string.IsNullOrEmpty(text);

        private Cached<Vector2> internalSize = new Cached<Vector2>();

        private float spaceWidth;

        private static TextureStore defaultFontStore;

        private TextureStore store;

        public SpriteText(TextureStore store = null)
        {
            this.store = store;
        }

        public override void Load()
        {
            base.Load();

            if (store == null)
                store = Game.Fonts;

            spaceWidth = getSprite('.')?.Width * 2 ?? 20;
        }

        private string text;
        public virtual string Text
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
        public bool TextFixedWidth
        {
            get { return constantWidth.HasValue; }
            set
            {
                if (value)
                    constantWidth = getSprite('5')?.Width + 1 ?? 20;
                else
                    constantWidth = null;
            }
        }

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

        private void refreshLayout()
        {
            internalSize.Refresh(delegate
            {
                Clear();

                if (string.IsNullOrEmpty(text))
                    return Vector2.Zero;

                foreach (char c in text)
                {
                    Drawable s;
                    if (c == ' ')
                        s = new Container() { Size = new Vector2(spaceWidth) };
                    else
                        s = getSprite(c);

                    Add(s);
                }

                return Vector2.Zero;
            });
        }

        private Sprite getSprite(char c) => new Sprite(getTexture(c));
        private Texture getTexture(char c) => store?.Get(getTextureName(c));
        private string getTextureName(char c) => $@"{c}";
    }
}
