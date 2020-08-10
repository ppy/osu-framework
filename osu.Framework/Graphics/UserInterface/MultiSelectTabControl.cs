// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// <see cref="BaseTabControl{T}"/> control which allows multiple tabs to be selected at the same time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MultiSelectTabControl<T> : BaseTabControl<T>
    {
        /// <summary>
        /// All selected tabs
        /// </summary>
        public IEnumerable<TabItem<T>> SelectedTabs => TabMap.Values.Where(v => v.Active.Value == true);

        public MultiSelectTabControl()
        {
            CanSelectMultipleTabs = true;
        }
    }
}
