// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Containers
{
    public class ClickableContainer : Container
    {
        private Action action;

        public Action Action
        {
            get
            {
                return action;
            }

            set
            {
                action = value;
                Enabled.Value = action != null;
            }
        }

        public readonly BindableBool Enabled = new BindableBool();

        protected override bool OnClick(InputState state)
        {
            if (Enabled.Value)
                Action?.Invoke();
            return true;
        }
    }
}
