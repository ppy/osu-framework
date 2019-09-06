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

        private Bindable<bool> currentBound;

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

                if (currentBound != null) current.UnbindFrom(currentBound);
                current.BindTo(currentBound = value);
            }
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!Current.Disabled)
            {
                Current.Value = !Current.Value;
                OnUserChange(Current.Value);
            }

            base.OnClick(e);
            return true;
        }

        /// <summary>
        /// Triggered when the value is changed based on end-user input to this control.
        /// </summary>
        protected virtual void OnUserChange(bool value)
        {
        }
    }
}
