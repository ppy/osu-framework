// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;

namespace osu.Framework.Graphics.UserInterface
{
    public class MenuItem
    {
        /// <summary>
        /// The text which this <see cref="MenuItem"/> displays.
        /// </summary>
        public readonly Bindable<string> Text = new Bindable<string>();

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
        public MenuItem(string text)
        {
            Text.Value = text;
        }

        /// <summary>
        /// Creates a new <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="action">The <see cref="Action"/> to perform when clicked.</param>
        public MenuItem(string text, Action action)
            : this(text)
        {
            Action.Value = action;
        }
    }
}
