// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Input;
using OpenTK.Graphics;
using OpenTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class CheckBox : FlowContainer, IStateful<CheckBoxState>
    {
        private CheckBoxState state = CheckBoxState.Unchecked;
        public CheckBoxState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;

                state = value;
                switch (state)
                {
                    case CheckBoxState.Checked:
                        OnChecked();
                        break;
                    case CheckBoxState.Unchecked:
                        OnUnchecked();
                        break;
                }
            }
        }

        public CheckBox()
        {
            Direction = FlowDirection.HorizontalOnly;
        }

        protected override bool OnClick(InputState state)
        {
            if (State == CheckBoxState.Checked)
            {
                State = CheckBoxState.Unchecked;
            }
            else
            {
                State = CheckBoxState.Checked;
            }
            base.OnClick(state);
            return true;
        }

        protected virtual void OnChecked()
        {
        }

        protected virtual void OnUnchecked()
        {
        }
    }

    public enum CheckBoxState
    {
        Checked,
        Unchecked
    }

    public class BasicCheckBox : CheckBox
    {
        protected virtual Drawable CheckedDrawable => new Box { Size = new Vector2(20, 20), Colour = Color4.Cyan };
        protected virtual Drawable UncheckedDrawable => new Box { Size = new Vector2(20, 20) };
        private AutoSizeContainer content;
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
            }
        }

        public BasicCheckBox()
        {
            LabelPadding = new MarginPadding
            {
                Left = 20
            };
            Children = new Drawable[]
            {
                content = new AutoSizeContainer()
            };
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            content.Add(UncheckedDrawable);
            Add(labelSpriteText = new SpriteText
            {
                Padding = LabelPadding,
                Text = labelText
            });
        }

        protected override void OnUnchecked()
        {
            content.Clear();
            content.Add(UncheckedDrawable);
        }

        protected override void OnChecked()
        {
            content.Clear();
            content.Add(CheckedDrawable);
        }
    }
}
