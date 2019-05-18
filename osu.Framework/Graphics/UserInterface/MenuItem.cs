// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Input.Commands;

namespace osu.Framework.Graphics.UserInterface
{
    public class MenuItem
    {
        /// <summary>
        /// The text which this <see cref="MenuItem"/> displays.
        /// </summary>
        public readonly Bindable<string> Text = new Bindable<string>(string.Empty);

        /// <summary>
        /// The <see cref="ICommand"/> that is performed when this <see cref="MenuItem"/> is clicked.
        /// </summary>
        public readonly Bindable<ICommand> Command = new Bindable<ICommand>();

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
        /// <param name="command">The <see cref="ICommand"/> to perform when clicked.</param>
        public MenuItem(string text, ICommand command)
            : this(text)
        {
            Command.Value = command;
        }
    }
}
