using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class SingleSelectTabControl<T> : BaseTabControl<T>
    {
        /// <summary>
        /// The currently selected <see cref="TabItem{T}"/>
        /// </summary>
        public TabItem<T> SelectedTab => LastSelectedTab;

        public SingleSelectTabControl()
        {
            CanSelectMultipleTabs = false;
        }
    }
}
