// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.UserInterface
{
    public class MenuItem
    {
        /// <summary>
        /// The text which this <see cref="MenuItem"/> displays.
        /// </summary>
        public readonly Bindable<LocalisableString> Text = new Bindable<LocalisableString>(string.Empty);

        /// <summary>
        /// The <see cref="Action"/> that is performed when this <see cref="MenuItem"/> is clicked.
        /// </summary>
        public readonly Bindable<Action> Action = new Bindable<Action>();

        /// <summary>
        /// A list of items which are to be displayed in a sub-menu originating from this <see cref="MenuItem"/>.
        /// </summary>
        public IReadOnlyList<MenuItem> Items = Array.Empty<MenuItem>();

        /// <summary>
        /// Creates a new <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="text">The text to display.</param>
        public MenuItem(LocalisableString text)
        {
            Text.Value = text;
        }

        /// <summary>
        /// Creates a new <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to perform when clicked.</param>
        protected MenuItem(Action action)
        {
            Action.Value = action;
        }

        /// <summary>
        /// Creates a new <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="action">The <see cref="Action"/> to perform when clicked.</param>
        public MenuItem(LocalisableString text, Action action)
            : this(text)
        {
            Action.Value = action;
        }
    }
}
