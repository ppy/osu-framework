// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class MenuBar : CompositeDrawable
    {
        /// <summary>
        /// The <see cref="FlowContainer{T}"/> which contains the <see cref="MenuBarItem"/> of this <see cref="MenuBar"/>.
        /// </summary>
        protected readonly FlowContainer<MenuBarItem> ItemFlow;

        public MenuBar()
        {
            AutoSizeAxes = Axes.Both;

            AddInternal(ItemFlow = new FillFlowContainer<MenuBarItem>
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal
            });
        }

        /// <summary>
        /// The <see cref="MenuBarItem"/>s which are to be displayed in this <see cref="MenuBar"/>.
        /// </summary>
        public IEnumerable<MenuBarItem> Items
        {
            set
            {
                ItemFlow.Clear();
                value?.ForEach(i => ItemFlow.Add(i));
            }
        }

        protected override bool OnMouseMove(InputState state)
        {
            var currentlyOpened = ItemFlow.FirstOrDefault(i => i.State == MenuState.Opened);

            // Don't handle mouse moves if there isn't a currently open item
            if (currentlyOpened == null)
                return false;

            var currentlyHovered = ItemFlow.FirstOrDefault(i => i.IsHovered);

            // Sanity check to make sure that we are hovering over an item - takes care of any spacing nonsense
            if (currentlyHovered == null)
                return false;

            if (currentlyOpened == currentlyHovered)
                return false;

            // We're hovering over an item other than the one currently opened - open the hovered item's menu and close the currently-opened one
            currentlyOpened.Close();
            currentlyHovered.Open();
            return true;
        }
    }
}