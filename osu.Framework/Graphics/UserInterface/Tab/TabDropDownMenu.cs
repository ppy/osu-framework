// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

// TODO: Hide header when no items in dropdown
namespace osu.Framework.Graphics.UserInterface.Tab
{
    // Keep abstract for now, until a generic styled header can be determined
    public abstract class TabDropDownMenu<T> : DropDownMenu<T>
    {
        internal float HeaderWidth => Header.Width;

        protected TabDropDownMenu()
        {
            RelativeSizeAxes = Axes.X;
            Header.Anchor = Anchor.TopRight;
            Header.Origin = Anchor.TopRight;
            ContentContainer.Anchor = Anchor.TopRight;
            ContentContainer.Origin = Anchor.TopRight;
        }

        // Consider making these menuitem manipulation internal
        public bool Contains(T val)
        {
            return ItemDictionary.ContainsKey(val);
        }

        public void HideItem(T val) {
            if (ItemDictionary.TryGetValue(val, out int index))
                ItemList[index]?.Hide();
        }

        public void ShowItem(T val) {
            if (ItemDictionary.TryGetValue(val, out int index))
                ItemList[index]?.Show();
        }

        // Don't give focus or it will cover tabs
        protected override bool OnFocus(InputState state) => false;
    }
}
