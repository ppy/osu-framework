// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

// TODO: Hide header when no items in dropdown
namespace osu.Framework.Graphics.UserInterface.Tab
{
    // Keep abstract for now, until a generic styled header can be determined
    public abstract class TabDropDownMenu<T> : DropDownMenu<T>
    {
        // These need to be set manually until there is a dynamic way to determine
        public abstract float HeaderWidth { get; }
        public abstract float HeaderHeight { get; }

        protected TabDropDownMenu()
        {
            RelativeSizeAxes = Axes.X;
            Header.Anchor = Anchor.TopRight;
            Header.Origin = Anchor.TopRight;
            ContentContainer.Anchor = Anchor.TopRight;
            ContentContainer.Origin = Anchor.TopRight;
        }

        internal void HideItem(T val)
        {
            int index;
            if (ItemDictionary.TryGetValue(val, out index))
                ItemList[index]?.Hide();
        }

        internal void ShowItem(T val)
        {
            int index;
            if (ItemDictionary.TryGetValue(val, out index))
                ItemList[index]?.Show();
        }

        // Don't give focus or it will cover tabs
        protected override bool OnFocus(InputState state) => false;
    }
}
