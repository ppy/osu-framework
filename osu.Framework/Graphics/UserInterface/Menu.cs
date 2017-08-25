// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.UserInterface
{
    public enum MenuState
    {
        Closed,
        Opened
    }

    /// <summary>
    /// A list of command or selection items.
    /// </summary>
    public class Menu<TItem> : Container, IStateful<MenuState>
        where TItem : MenuItem
    {
        public readonly Box Background;
        public readonly FillFlowContainer<TItem> ItemsContainer;
        public readonly ScrollContainer ScrollContainer;

        public Menu()
        {
            Masking = true;

            Children = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                ScrollContainer = new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = false,
                    Child = ItemsContainer = new FillFlowContainer<TItem>
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

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// The current state of this <see cref="Menu{TItem}"/>.
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
        /// The maximum height allowable by this <see cref="Menu{TItem}"/>.
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

        protected virtual void UpdateContentHeight() => Height = Math.Min(ItemsContainer.Height, MaxHeight);

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
}
