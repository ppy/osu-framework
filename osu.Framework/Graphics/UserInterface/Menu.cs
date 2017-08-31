// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class Menu : CompositeDrawable, IStateful<MenuState>
    {
        /// <summary>
        /// The delay before opening sub-menus when menu items are hovered.
        /// </summary>
        private const double hover_open_delay = 500;

        public event Action<MenuState> StateChanged;

        /// <summary>
        /// The <see cref="Container{T}"/> that contains the content of this <see cref="Menu"/>.
        /// </summary>
        protected readonly ScrollContainer<Container<DrawableMenuItem>> ContentContainer;

        /// <summary>
        /// The <see cref="Container{T}"/> that contains the items of this <see cref="Menu"/>.
        /// </summary>
        protected readonly FillFlowContainer<DrawableMenuItem> ItemsContainer;

        /// <summary>
        /// Gets the item representations contained by this <see cref="Menu"/>.
        /// </summary>
        protected IReadOnlyList<DrawableMenuItem> Children => ItemsContainer;

        private readonly Lazy<Menu> lazySubMenu;
        private Menu subMenu => lazySubMenu.Value;
        private Menu parentMenu;

        private readonly Box background;
        private readonly Direction direction;

        public Menu(Direction direction)
        {
            this.direction = direction;

            Container<Menu> subMenuContainer;
            InternalChildren = new Drawable[]
            {
                new Container
                {
                    Name = "Our contents",
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black
                        },
                        ContentContainer = new ScrollContainer<Container<DrawableMenuItem>>(direction)
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = false,
                            Child = ItemsContainer = new FillFlowContainer<DrawableMenuItem> { Direction = direction == Direction.Horizontal ? FillDirection.Horizontal : FillDirection.Vertical }
                        }
                    }
                },
                subMenuContainer = new Container<Menu>
                {
                    Name = "Sub menu container",
                    AutoSizeAxes = Axes.Both
                }
            };

            lazySubMenu = new Lazy<Menu>(() =>
            {
                var menu = CreateSubMenu();
                subMenuContainer.Add(menu);
                return menu;
            });

            switch (direction)
            {
                case Direction.Horizontal:
                    ItemsContainer.AutoSizeAxes = Axes.X;
                    break;
                case Direction.Vertical:
                    ItemsContainer.AutoSizeAxes = Axes.Y;
                    break;
            }

            // The menu will provide a valid size for the items container based on our own size
            ItemsContainer.RelativeSizeAxes = Axes.Both & ~ItemsContainer.AutoSizeAxes;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateState();
        }

        /// <summary>
        /// Gets or sets the <see cref="MenuItem"/>s contained within this <see cref="Menu"/>.
        /// </summary>
        public IReadOnlyList<MenuItem> Items
        {
            get { return ItemsContainer.Select(r => r.Item).ToList(); }
            set
            {
                Clear();
                value?.ForEach(Add);
            }
        }

        /// <summary>
        /// Gets or sets the background colour of this <see cref="Menu"/>.
        /// </summary>
        public Color4 BackgroundColour
        {
            get { return background.Colour; }
            set { background.Colour = value; }
        }

        /// <summary>
        /// Gets or sets whether the scroll bar of this <see cref="Menu"/> should be visible.
        /// </summary>
        public bool ScrollbarVisible
        {
            get { return ContentContainer.ScrollbarVisible; }
            set { ContentContainer.ScrollbarVisible = value; }
        }

        private float maxWidth = float.MaxValue;
        /// <summary>
        /// Gets or sets the maximum allowable width by this <see cref="Menu"/>.
        /// </summary>
        public float MaxWidth
        {
            get { return maxWidth; }
            set
            {
                if (Precision.AlmostEquals(maxWidth, value))
                    return;
                maxWidth = value;

                // Todo: Invalidate
            }
        }

        private float maxHeight = float.PositiveInfinity;
        /// <summary>
        /// Gets or sets the maximum allowable height by this <see cref="Menu"/>.
        /// </summary>
        public float MaxHeight
        {
            get { return maxHeight; }
            set
            {
                if (Precision.AlmostEquals(maxHeight, value))
                    return;
                maxHeight = value;

                // Todo: Invalidate
            }
        }

        private bool alwaysOpen;
        /// <summary>
        /// Gets or sets whether this <see cref="Menu"/> should always be open.
        /// </summary>
        public bool AlwaysOpen
        {
            get { return alwaysOpen; }
            set
            {
                if (alwaysOpen == value)
                    return;
                alwaysOpen = value;

                if (value && state == MenuState.Closed)
                    state = MenuState.Opened;

                if (!IsLoaded)
                    return;

                updateState();
            }
        }

        /// <summary>
        /// Whether a click is required to open sub-<see cref="Menu"/> of this <see cref="Menu"/>.
        /// </summary>
        public bool RequireClickToOpen = true;

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu"/>.
        /// </summary>
        public virtual MenuState State
        {
            get { return state; }
            set
            {
                if (AlwaysOpen && value == MenuState.Closed)
                    return;

                if (state == value)
                    return;
                state = value;

                if (!IsLoaded)
                    return;

                updateState();
            }
        }

        private void updateState()
        {
            switch (State)
            {
                case MenuState.Closed:
                    AnimateClose();

                    if (AlwaysOpen)
                        break;

                    if (HasFocus)
                        GetContainingInputManager().ChangeFocus(null);
                    break;
                case MenuState.Opened:
                    AnimateOpen();

                    if (AlwaysOpen)
                        break;

                    //schedule required as we may not be present currently.
                    Schedule(() =>
                    {
                        if (State == MenuState.Opened)
                            GetContainingInputManager().ChangeFocus(this);
                    });
                    break;
            }

            StateChanged?.Invoke(State);
        }

        /// <summary>
        /// Adds a <see cref="MenuItem"/> to this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to add.</param>
        public virtual void Add(MenuItem item)
        {
            var drawableItem = CreateDrawableMenuItem(item);
            drawableItem.AutoSizeAxes = ItemsContainer.AutoSizeAxes;
            drawableItem.RelativeSizeAxes = ItemsContainer.RelativeSizeAxes;
            drawableItem.Clicked = menuItemClicked;
            drawableItem.Hovered = menuItemHovered;

            ItemsContainer.Add(drawableItem);

            subMenu.parentMenu = this;
        }

        /// <summary>
        /// Removes a <see cref="MenuItem"/> from this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(MenuItem item) => ItemsContainer.RemoveAll(d => d.Item == item) > 0;

        /// <summary>
        /// Clears all <see cref="MenuItem"/>s in this <see cref="Menu"/>.
        /// </summary>
        public void Clear() => ItemsContainer.Clear();

        /// <summary>
        /// Opens this <see cref="Menu"/>.
        /// </summary>
        public void Open() => State = MenuState.Opened;

        /// <summary>
        /// Closes this <see cref="Menu"/>.
        /// </summary>
        public void Close() => State = MenuState.Closed;

        /// <summary>
        /// Toggles the state of this <see cref="Menu"/>.
        /// </summary>
        public void Toggle() => State = State == MenuState.Closed ? MenuState.Opened : MenuState.Closed;

        /// <summary>
        /// Animates the opening of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void AnimateOpen() => Show();

        /// <summary>
        /// Animates the closing of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void AnimateClose() => Hide();

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // Todo: The following should use invalidate

            // Our children will be relatively-sized on the axis separate to the menu direction, so we need to compute
            // that size ourselves, based on the content size of our children, to give them a valid relative size

            float width = 0;
            float height = 0;

            foreach (var item in Children)
            {
                width = Math.Max(width, item.ContentDrawWidth);
                height = Math.Max(height, item.ContentDrawHeight);
            }

            // When scrolling in one direction, ItemsContainer is auto-sized in that direction and relative-sized in the other
            // In the case of the auto-sized direction, we want to use its size. In the case of the relative-sized direction, we want
            // to use the (above) computed size.
            width = direction == Direction.Horizontal ? ItemsContainer.Width : width;
            height = direction == Direction.Vertical ? ItemsContainer.Height : height;

            width = Math.Min(MaxWidth, width);
            height = Math.Min(MaxHeight, height);

            // Regardless of the above result, if we are relative-sizing, just use the stored width/height
            width = (RelativeSizeAxes & Axes.X) > 0 ? Width : width;
            height = (RelativeSizeAxes & Axes.Y) > 0 ? Height : height;

            Size = new Vector2(width, height);
        }

        #region Hover/Focus logic
        private void menuItemClicked(DrawableMenuItem item)
        {
            // We only want to close the sub-menu if we're not a sub menu - if we are a sub menu
            // then clicks should instead cause the sub menus to instantly show up
            if (RequireClickToOpen && subMenu.State == MenuState.Opened)
            {
                subMenu.Close();
                return;
            }

            // Check if there is a sub menu to display
            if (item.Item.Items?.Count == 0)
            {
                // This item must have attempted to invoke an action - close all menus
                closeAll();
                return;
            }

            // Make sure we only show one level of the submenu if we re-open
            if (subMenu.State == MenuState.Closed)
                subMenu.subMenu?.Close();

            openDelegate?.Cancel();
            subMenu.openAt(item);
        }

        private ScheduledDelegate openDelegate;
        private void menuItemHovered(DrawableMenuItem item)
        {
            // If we're not a sub-menu, then hover shouldn't display a sub-menu unless an item is clicked
            if (RequireClickToOpen && subMenu.State == MenuState.Closed)
                return;

            // Make sure we only show one level of the submenu
            subMenu.subMenu?.Close();

            openDelegate?.Cancel();
            openDelegate = Scheduler.AddDelayed(() =>
            {
                if (item.IsHovered)
                    subMenu.openAt(item);
            }, RequireClickToOpen ? 0 : hover_open_delay);
        }

        public override bool AcceptsFocus => true;
        protected override bool OnClick(InputState state) => true;

        protected override void OnFocusLost(InputState state)
        {
            // For the case where this menu is a sub-menu, and one of the items that has a sub-menu is being hovered,
            // the focus will be transferred to that sub-menu while this menu will receive OnFocusLost
            // If this is not done then this menu will close and the sub-menu won't be shown.
            if (anySubMenuOpened)
                return;

            // This covers the case where one of the parent menus are hovered, which will close the 2nd-level sub-menus (see menuItemHovered)
            // which causes this sub-menu to lose focus, and requires focus to be transferred to the parent's 1st-level sub-menu
            // If this is not done, a menu with focus won't exist, and clicks outside the menus to close them won't work
            var hoveredParent = findHoveredParent;
            if (hoveredParent != null)
            {
                hoveredParent.subMenu.Schedule(() =>
                {
                    if (hoveredParent.subMenu.State == MenuState.Opened)
                        GetContainingInputManager().ChangeFocus(hoveredParent.subMenu);
                });

                return;
            }

            // At this point we are guaranteed to have lost focus due to clicks outside the menu structure
            closeAll();
        }

        /// <summary>
        /// Closes all open <see cref="Menu"/>s.
        /// </summary>
        private void closeAll()
        {
            var iterator = this;
            while (iterator != null)
            {
                iterator.Close();
                iterator = iterator.parentMenu;
            }
        }

        /// <summary>
        /// Opens a <see cref="Menu"/> with the items of <paramref name="item"/> and at a position offset from <paramref name="item"/>.
        /// </summary>
        /// <param name="item">The <see cref="DrawableMenuItem"/> which the sub-<see cref="Menu"/> should display for.</param>
        private void openAt(DrawableMenuItem item)
        {
            Items = item.Item.Items;
            Position = new Vector2(parentMenu.direction == Direction.Vertical ? parentMenu.Width : item.X, parentMenu.direction == Direction.Horizontal ? parentMenu.Height : item.Y);
            Open();
        }

        /// <summary>
        /// Searches up through the parent <see cref="Menu"/>s and returns the first one that is hovered,
        /// or null if there is no hovered <see cref="Menu"/>.
        /// </summary>
        private Menu findHoveredParent
        {
            get
            {
                if (parentMenu == null)
                    return null;

                if (parentMenu.IsHovered)
                    return parentMenu;

                return parentMenu.findHoveredParent;
            }
        }

        /// <summary>
        /// Checks if any sub-<see cref="Menu"/>s of this <see cref="Menu"/> are open.
        /// </summary>
        private bool anySubMenuOpened => subMenu?.State == MenuState.Opened;
        #endregion

        /// <summary>
        /// Creates a sub-menu for <see cref="MenuItem.Items"/> of <see cref="MenuItem"/>s added to this <see cref="Menu"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual Menu CreateSubMenu() => new Menu(Direction.Vertical)
        {
            Anchor = direction == Direction.Horizontal ? Anchor.BottomLeft : Anchor.TopRight,
            RequireClickToOpen = false
        };

        /// <summary>
        /// Creates the visual representation for a <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> that is to be visualised.</param>
        /// <returns>The visual representation.</returns>
        protected virtual DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableMenuItem(item);

        #region DrawableMenuItem
        protected class DrawableMenuItem : CompositeDrawable
        {
            /// <summary>
            /// Invoked when this <see cref="DrawableMenuItem"/> is clicked. This occurs regardless of whether or not <see cref="MenuItem.Action"/> was
            /// invoked or not, or whether <see cref="Item"/> contains any sub-<see cref="MenuItem"/>s.
            /// </summary>
            public Action<DrawableMenuItem> Clicked;

            /// <summary>
            /// Invoked when this <see cref="DrawableMenuItem"/> is hovered. This runs one update frame behind the actual hover event.
            /// </summary>
            public Action<DrawableMenuItem> Hovered;

            /// <summary>
            /// The <see cref="MenuItem"/> which this <see cref="DrawableMenuItem"/> represents.
            /// </summary>
            public readonly MenuItem Item;

            /// <summary>
            /// The content of this <see cref="DrawableMenuItem"/>, created through <see cref="CreateContent"/>.
            /// </summary>
            protected readonly Drawable Content;

            /// <summary>
            /// The background of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            protected readonly Box Background;

            /// <summary>
            /// The foreground of this <see cref="DrawableMenuItem"/>. This contains the content of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            protected readonly Container Foreground;

            public DrawableMenuItem(MenuItem item)
            {
                Item = item;

                InternalChildren = new Drawable[]
                {
                    Background = new Box { RelativeSizeAxes = Axes.Both },
                    Foreground = new Container { Child = Content = CreateContent() },
                };

                var textContent = Content as IHasText;
                if (textContent != null)
                {
                    textContent.Text = item.Text;
                    Item.Text.ValueChanged += newText => textContent.Text = newText;
                }
            }

            private Color4 backgroundColour = Color4.DarkSlateGray;
            /// <summary>
            /// Gets or sets the default background colour.
            /// </summary>
            public Color4 BackgroundColour
            {
                get { return backgroundColour; }
                set
                {
                    backgroundColour = value;
                    UpdateBackgroundColour();
                }
            }

            private Color4 foregroundColour = Color4.White;
            /// <summary>
            /// Gets or sets the default foreground colour.
            /// </summary>
            public Color4 ForegroundColour
            {
                get { return foregroundColour; }
                set
                {
                    foregroundColour = value;
                    UpdateForegroundColour();
                }
            }

            private Color4 backgroundColourHover = Color4.DarkGray;
            /// <summary>
            /// Gets or sets the background colour when this <see cref="DrawableMenuItem"/> is hovered.
            /// </summary>
            public Color4 BackgroundColourHover
            {
                get { return backgroundColourHover; }
                set
                {
                    backgroundColourHover = value;
                    UpdateBackgroundColour();
                }
            }

            private Color4 foregroundColourHover = Color4.White;
            /// <summary>
            /// Gets or sets the foreground colour when this <see cref="DrawableMenuItem"/> is hovered.
            /// </summary>
            public Color4 ForegroundColourHover
            {
                get { return foregroundColourHover; }
                set
                {
                    foregroundColourHover = value;
                    UpdateForegroundColour();
                }
            }

            public override Axes RelativeSizeAxes
            {
                get { return base.RelativeSizeAxes; }
                set
                {
                    base.RelativeSizeAxes = value;
                    Foreground.RelativeSizeAxes = value;
                }
            }

            public new Axes AutoSizeAxes
            {
                get { return base.AutoSizeAxes; }
                set
                {
                    base.AutoSizeAxes = value;
                    Foreground.AutoSizeAxes = value;
                }
            }

            /// <summary>
            /// The draw width of the text of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            public float ContentDrawWidth => Content.DrawWidth;

            /// <summary>
            /// The draw width of the text of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            public float ContentDrawHeight => Content.DrawHeight;

            /// <summary>
            /// Called after the <see cref="BackgroundColour"/> is modified or the hover state changes.
            /// </summary>
            protected virtual void UpdateBackgroundColour()
            {
                Background.FadeColour(IsHovered ? BackgroundColourHover : BackgroundColour);
            }

            /// <summary>
            /// Called after the <see cref="ForegroundColour"/> is modified or the hover state changes.
            /// </summary>
            protected virtual void UpdateForegroundColour()
            {
                Foreground.FadeColour(IsHovered ? ForegroundColourHover : ForegroundColour);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Background.Colour = BackgroundColour;
                Foreground.Colour = ForegroundColour;
            }

            protected override bool OnHover(InputState state)
            {
                UpdateBackgroundColour();
                UpdateForegroundColour();

                Schedule(() =>
                {
                    if (IsHovered)
                        Hovered?.Invoke(this);
                });

                return base.OnHover(state);
            }

            protected override void OnHoverLost(InputState state)
            {
                UpdateBackgroundColour();
                UpdateForegroundColour();
                base.OnHoverLost(state);
            }

            protected override bool OnClick(InputState state)
            {
                if (Item.Items?.Count == 0)
                {
                    if (Item.Action.Disabled)
                        return false;

                    Item.Action.Value?.Invoke();
                }

                Clicked?.Invoke(this);

                return true;
            }

            /// <summary>
            /// Creates the content which will be displayed in this <see cref="DrawableMenuItem"/>.
            /// If the <see cref="Drawable"/> returned implements <see cref="IHasText"/>, the text will be automatically
            /// updated when the <see cref="MenuItem.Text"/> is updated.
            /// </summary>
            protected virtual Drawable CreateContent() => new SpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                TextSize = 17,
            };
        }
        #endregion
    }

    public enum MenuState
    {
        Closed,
        Opened
    }
}
