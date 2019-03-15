// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Containers
{
    public class ClickableContainer : Container
    {
        private Action action;

        public Action Action
        {
            get => action;
            set
            {
                action = value;
                Enabled.Value = action != null;
            }
        }

        public readonly BindableBool Enabled = new BindableBool();

        protected override bool Handle(PositionalEvent e)
        {
            switch (e)
            {
                case ClickEvent _:
                    if (Enabled.Value)
                        Action?.Invoke();
                    return true;

                default:
                    return base.Handle(e);
            }
        }
    }
}
