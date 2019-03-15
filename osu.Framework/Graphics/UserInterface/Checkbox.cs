// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// An abstract class that implements the functionality of a checkbox.
    /// </summary>
    public abstract class Checkbox : Container, IHasCurrentValue<bool>
    {
        private readonly Bindable<bool> current = new Bindable<bool>();

        /// <summary>
        /// A bindable that holds the value if the checkbox is checked or not.
        /// </summary>
        public Bindable<bool> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                current.UnbindBindings();
                current.BindTo(value);
            }
        }

        protected override bool Handle(PositionalEvent e)
        {
            switch (e)
            {
                case ClickEvent clickEvent:
                    if (!Current.Disabled)
                        Current.Value = !Current.Value;

                    base.Handle(clickEvent);
                    return true;

                default:
                    return base.Handle(e);
            }
        }
    }
}
