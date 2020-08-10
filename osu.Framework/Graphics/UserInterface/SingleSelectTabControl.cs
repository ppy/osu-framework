// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// <see cref="BaseTabControl{T}"/> control which allows only 1 item to be selected at any moment.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingleSelectTabControl<T> : BaseTabControl<T>
    {
        /// <summary>
        /// The currently selected <see cref="TabItem{T}"/>
        /// </summary>
        public TabItem<T> SelectedTab => base.LastSelectedTab;

        public SingleSelectTabControl()
        {
            CanSelectMultipleTabs = false;
        }
    }
}
