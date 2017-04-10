// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class Checkbox : Container, IHasCurrentValue<bool>
    {
        private readonly Bindable<bool> current = new Bindable<bool>();

        public Bindable<bool> Current
        {
            get { return current; }
            set { current.BindTo(value); }
        }

        protected override bool OnClick(InputState state)
        {
            Current.Value = !Current;
            base.OnClick(state);
            return true;
        }
    }
}
