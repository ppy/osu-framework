// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class CheckBox : Container, IStateful<CheckBoxState>
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

        protected override bool OnClick(InputState state)
        {
            State = State == CheckBoxState.Checked ? CheckBoxState.Unchecked : CheckBoxState.Checked;
            base.OnClick(state);
            return true;
        }

        protected abstract void OnChecked();

        protected abstract void OnUnchecked();
    }

    public enum CheckBoxState
    {
        Checked,
        Unchecked
    }
}
