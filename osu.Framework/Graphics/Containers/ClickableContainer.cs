// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Containers
{
    public class ClickableContainer : Container, IHandleMouseButtons
    {
        public Action Action;
        public readonly BindableBool Enabled = new BindableBool(true);

        public virtual bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            return false;
        }

        public virtual bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            return false;
        }

        public virtual bool OnClick(InputState state)
        {
            if (Enabled.Value)
                Action?.Invoke();
            return true;
        }

        public virtual bool OnDoubleClick(InputState state)
        {
            return false;
        }
    }
}
