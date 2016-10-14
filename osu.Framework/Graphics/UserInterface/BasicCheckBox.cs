// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{

    public class BasicCheckBox : CheckBox
    {
        protected virtual Drawable CreateCheckedDrawable() => new Box { Size = new Vector2(20, 20), Colour = Color4.Cyan, Depth = float.MinValue };
        protected virtual Drawable CreateUncheckedDrawable() => new Box { Size = new Vector2(20, 20), Depth = float.MinValue };
        private Drawable checkedDrawable;
        private Drawable uncheckedDrawable;
        private SpriteText labelSpriteText;
        private string labelText = string.Empty;
        public string LabelText
        {
            get { return labelText; }
            set
            {
                labelText = value;
                if (labelSpriteText != null)
                    labelSpriteText.Text = labelText;
            }
        }
        private MarginPadding labelPadding;
        public MarginPadding LabelPadding
        {
            get { return labelPadding; }
            set
            {
                labelPadding = value;
                if (labelSpriteText != null)
                    labelSpriteText.Padding = labelPadding;
            }
        }

        public BasicCheckBox()
        {
            LabelPadding = new MarginPadding
            {
                Left = 20
            };
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            checkedDrawable = CreateCheckedDrawable();
            uncheckedDrawable = CreateUncheckedDrawable();

            Children = new Drawable[]
            {
                uncheckedDrawable,
                labelSpriteText = new SpriteText
                {
                    Padding = LabelPadding,
                    Text = labelText
                }
            };
        }

        protected override void OnUnchecked()
        {
            Remove(checkedDrawable);
            Add(uncheckedDrawable);
        }

        protected override void OnChecked()
        {
            Remove(uncheckedDrawable);
            Add(checkedDrawable);
        }
    }
}