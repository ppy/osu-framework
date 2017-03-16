// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;

namespace osu.Framework.Graphics.UserInterface.Tab
{
    public abstract class TabDropDownMenu<T> : DropDownMenu<T>
    {
        protected TabDropDownMenu()
        {
            RelativeSizeAxes = Axes.X;
            Header.Anchor = Anchor.TopRight;
            Header.Origin = Anchor.TopRight;
            ContentContainer.Anchor = Anchor.TopRight;
            ContentContainer.Origin = Anchor.TopRight;
        }

        internal float HeaderHeight
        {
            get { return Header.DrawHeight; }
            set { Header.Height = value; }
        }

        internal float HeaderWidth
        {
            get { return Header.DrawWidth; }
            set { Header.Width = value; }
        }

        internal void HideItem(T val)
        {
            int index;
            if (ItemDictionary.TryGetValue(val, out index))
                ItemList[index]?.Hide();

            updateAlphaVisibility();
        }

        internal void ShowItem(T val)
        {
            int index;
            if (ItemDictionary.TryGetValue(val, out index))
                ItemList[index]?.Show();

            updateAlphaVisibility();
        }

        private void updateAlphaVisibility() => Header.Alpha = ItemList.Any(i => i.IsPresent) ? 1 : 0;
    }
}
