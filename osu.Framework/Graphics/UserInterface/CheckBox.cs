// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Input;
using OpenTK.Graphics;
using System;
using OpenTK;
using System.Collections.Generic;

namespace osu.Framework.Graphics.UserInterface
{
    public class CheckBox : FlowContainer, IStateful<CheckBoxState>
    {

        public Drawable CheckedDrawable = new Box { Size = new Vector2(20, 20), Colour = Color4.Cyan };
        public Drawable UncheckedDrawable = new Box { Size = new Vector2(20, 20) };
        private AutoSizeContainer visualsCont;

        private List<Drawable> labels = new List<Drawable>();
        public IEnumerable<Drawable> Labels
        {
            get { return labels; }
            set
            {
                if (IsLoaded)
                {
                    foreach (Drawable d in value)
                        AddLabel(d);
                }
                else
                {
                    foreach (Drawable d in value)
                        labels.Add(d);
                }
            }
        }

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
                        CheckedAction?.Invoke();
                        visualsCont.Remove(UncheckedDrawable);
                        visualsCont.Add(CheckedDrawable);
                        break;
                    case CheckBoxState.Unchecked:
                        UncheckedAction?.Invoke();
                        visualsCont.Remove(CheckedDrawable);
                        visualsCont.Add(UncheckedDrawable);
                        break;
                }
            }
        }

        public Action CheckedAction;
        public Action UncheckedAction;

        public CheckBox()
        {
            Direction = FlowDirection.HorizontalOnly;
            Children = new Drawable[]
            {
                visualsCont = new AutoSizeContainer()
            };
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            visualsCont.Add(UncheckedDrawable);
            foreach (Drawable d in labels)
                Add(d);
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

        public void AddLabel(Drawable d)
        {
            Add(d);
            labels.Add(d);
        }

        public void RemoveLabel(Drawable d)
        {
            Remove(d);
            labels.Remove(d);
        }
    }

    public enum CheckBoxState
    {
        Checked,
        Unchecked
    }

}
