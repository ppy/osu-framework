// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A list of command or selection items.
    /// </summary>
    public class Menu<TItem> : CompositeDrawable, IStateful<MenuState>
        where TItem : MenuItem
    {
        /// <summary>
        /// Gets or sets the <see cref="TItem"/>s contained within this <see cref="Menu{TItem}"/>.
        /// </summary>
        public IReadOnlyList<TItem> Items
        {
            get { return itemsContainer; }
            set { itemsContainer.Children = value; }
        }

        /// <summary>
        /// Gets or sets the corner radius of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public new float CornerRadius
        {
            get { return base.CornerRadius; }
            set { base.CornerRadius = value; }
        }

        /// <summary>
        /// Gets or sets whether the scroll bar of this <see cref="Menu{TItem}"/> is visible.
        /// </summary>
        public bool ScrollbarVisible
        {
            get { return scrollContainer.ScrollbarVisible; }
            set { scrollContainer.ScrollbarVisible = value; }
        }

        /// <summary>
        /// Gets or sets the background colour of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public Color4 BackgroundColour
        {
            get { return background.Colour; }
            set { background.Colour = value; }
        }

        private readonly Box background;
        private readonly ScrollContainer scrollContainer;
        private readonly FillFlowContainer<TItem> itemsContainer;

        public Menu()
        {
            Masking = true;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                scrollContainer = new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = false,
                    Child = itemsContainer = new FillFlowContainer<TItem>
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (State == MenuState.Closed)
                AnimateClose();
            else
                AnimateOpen();
        }

        /// <summary>
        /// Adds a <see cref="TItem"/> to this <see cref="Menu{TItem}"/>.
        /// </summary>
        /// <param name="item">The <see cref="TItem"/> to add.</param>
        public void Add(TItem item) => itemsContainer.Add(item);

        /// <summary>
        /// Removes a <see cref="TItem"/> from this <see cref="Menu{TItem}"/>.
        /// </summary>
        /// <param name="item">The <see cref="TItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(TItem item) => itemsContainer.Remove(item);

        /// <summary>
        /// Clears all <see cref="TItem"/>s in this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Clear() => itemsContainer.Clear();

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public MenuState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;
                state = value;

                switch (value)
                {
                    case MenuState.Closed:
                        AnimateClose();
                        if (HasFocus)
                            GetContainingInputManager().ChangeFocus(null);
                        break;
                    case MenuState.Opened:
                        AnimateOpen();

                        //schedule required as we may not be present currently.
                        Schedule(() => GetContainingInputManager().ChangeFocus(this));
                        break;
                }

                UpdateContentHeight();
            }
        }

        /// <summary>
        /// Opens this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Open() => State = MenuState.Opened;

        /// <summary>
        /// Closes this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Close() => State = MenuState.Closed;

        /// <summary>
        /// Toggles the state of this <see cref="Menu{TItem}"/>.
        /// </summary>
        public void Toggle() => State = State == MenuState.Closed ? MenuState.Opened : MenuState.Closed;

        private float maxHeight = float.MaxValue;
        /// <summary>
        /// Gets or sets maximum height allowable by this <see cref="Menu{TItem}"/>.
        /// </summary>
        public float MaxHeight
        {
            get { return maxHeight; }
            set
            {
                maxHeight = value;
                UpdateContentHeight();
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            UpdateContentHeight();
        }

        protected virtual void UpdateContentHeight() => Height = Math.Min(itemsContainer.Height, MaxHeight);

        /// <summary>
        /// Animates the opening of this <see cref="Menu{TItem}"/>.
        /// </summary>
        protected virtual void AnimateOpen() => Show();

        /// <summary>
        /// Animates the closing of this <see cref="Menu{TItem}"/>.
        /// </summary>
        protected virtual void AnimateClose() => Hide();

        public override bool AcceptsFocus => true;
        protected override bool OnClick(InputState state) => true;
        protected override void OnFocusLost(InputState state) => State = MenuState.Closed;
    }

    public class Menu : Menu<MenuItem>
    {
    }

    public enum MenuState
    {
        Closed,
        Opened
    }
}
