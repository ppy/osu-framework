// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class Checkbox : Container, IStateful<CheckboxState>
    {
        private CheckboxState state = CheckboxState.Unchecked;

        public CheckboxState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;

                state = value;
                switch (state)
                {
                    case CheckboxState.Checked:
                        OnChecked();
                        break;
                    case CheckboxState.Unchecked:
                        OnUnchecked();
                        break;
                }
            }
        }

        protected override bool OnClick(InputState state)
        {
            State = State == CheckboxState.Checked ? CheckboxState.Unchecked : CheckboxState.Checked;
            base.OnClick(state);
            return true;
        }

        protected abstract void OnChecked();

        protected abstract void OnUnchecked();
    }

    public enum CheckboxState
    {
        Checked,
        Unchecked
    }
}
