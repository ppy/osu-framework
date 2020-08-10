using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace osu.Framework.Graphics.UserInterface
{
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
