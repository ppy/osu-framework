// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Extensions.IEnumerableExtensions;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public class Menu : CompositeDrawable, IStateful<MenuState>
    {
        /// <summary>
        /// Invoked when this <see cref="Menu"/>'s <see cref="State"/> changes.
        /// </summary>
        public event Action<MenuState> StateChanged;

        /// <summary>
        /// Gets or sets the delay before opening sub-<see cref="Menu"/>s when menu items are hovered.
        /// </summary>
        protected double HoverOpenDelay = 100;

        /// <summary>
        /// Whether this menu is always displayed in an open state (ie. a menu bar).
        /// Clicks are required to activate <see cref="DrawableMenuItem"/>.
        /// </summary>
        protected readonly bool TopLevelMenu;

        /// <summary>
        /// The <see cref="Container{T}"/> that contains the content of this <see cref="Menu"/>.
        /// </summary>
        protected readonly ScrollContainer<Container<DrawableMenuItem>> ContentContainer;

        /// <summary>
        /// The <see cref="Container{T}"/> that contains the items of this <see cref="Menu"/>.
        /// </summary>
        protected readonly FillFlowContainer<DrawableMenuItem> ItemsContainer;

        /// <summary>
        /// The container that provides the masking effects for this <see cref="Menu"/>.
        /// </summary>
        protected readonly Container MaskingContainer;

        /// <summary>
        /// Gets the item representations contained by this <see cref="Menu"/>.
        /// </summary>
        protected IReadOnlyList<DrawableMenuItem> Children => ItemsContainer;

        protected readonly Direction Direction;

        private Menu parentMenu;
        private Menu submenu;

        private readonly Box background;

        private Cached sizeCache = new Cached();

        private readonly Container<Menu> submenuContainer;

        /// <summary>
        /// Constructs a menu.
        /// </summary>
        /// <param name="direction">The direction of layout for this menu.</param>
        /// <param name="topLevelMenu">Whether the resultant menu is always displayed in an open state (ie. a menu bar).</param>
        public Menu(Direction direction, bool topLevelMenu = false)
        {
            Direction = direction;
            TopLevelMenu = topLevelMenu;

            if (topLevelMenu)
                state = MenuState.Open;

            InternalChildren = new Drawable[]
            {
                MaskingContainer = new Container
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
                submenuContainer = new Container<Menu>
                {
                    Name = "Sub menu container",
                    AutoSizeAxes = Axes.Both
                }
            };

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

                sizeCache.Invalidate();
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

                sizeCache.Invalidate();
            }
        }

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu"/>.
        /// </summary>
        public virtual MenuState State
        {
            get { return state; }
            set
            {
                if (TopLevelMenu)
                {
                    submenu?.Close();
                    return;
                }

                if (state == value)
                    return;
                state = value;

                updateState();
                StateChanged?.Invoke(State);
            }
        }

        private void updateState()
        {
            if (!IsLoaded)
                return;

            submenu?.Close();

            switch (State)
            {
                case MenuState.Closed:
                    AnimateClose();
                    break;
                case MenuState.Open:
                    AnimateOpen();
                    if (!TopLevelMenu)
                        // We may not be present at this point, so must run on the next frame.
                        Schedule(delegate
                        {
                            if (State == MenuState.Open) GetContainingInputManager().ChangeFocus(this);
                        });
                    break;
            }

            sizeCache.Invalidate();
        }

        /// <summary>
        /// Adds a <see cref="MenuItem"/> to this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to add.</param>
        public virtual void Add(MenuItem item)
        {
            var drawableItem = CreateDrawableMenuItem(item);
            drawableItem.Clicked = menuItemClicked;
            drawableItem.Hovered = menuItemHovered;
            drawableItem.StateChanged += s => itemStateChanged(drawableItem, s);

            drawableItem.SetFlowDirection(Direction);

            ItemsContainer.Add(drawableItem);
        }

        private void itemStateChanged(DrawableMenuItem item, MenuItemState state)
        {
            if (state != MenuItemState.Selected) return;

            if (item != selectedItem && selectedItem != null)
                selectedItem.State = MenuItemState.NotSelected;
            selectedItem = item;
        }

        /// <summary>
        /// Removes a <see cref="MenuItem"/> from this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(MenuItem item)
        {
            bool result = ItemsContainer.RemoveAll(d => d.Item == item) > 0;
            sizeCache.Invalidate();

            return result;
        }

        /// <summary>
        /// Clears all <see cref="MenuItem"/>s in this <see cref="Menu"/>.
        /// </summary>
        public void Clear()
        {
            ItemsContainer.Clear();
            updateState();
        }

        /// <summary>
        /// Opens this <see cref="Menu"/>.
        /// </summary>
        public void Open() => State = MenuState.Open;

        /// <summary>
        /// Closes this <see cref="Menu"/>.
        /// </summary>
        public void Close() => State = MenuState.Closed;

        /// <summary>
        /// Toggles the state of this <see cref="Menu"/>.
        /// </summary>
        public void Toggle() => State = State == MenuState.Closed ? MenuState.Open : MenuState.Closed;

        /// <summary>
        /// Animates the opening of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void AnimateOpen() => Show();

        /// <summary>
        /// Animates the closing of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void AnimateClose() => Hide();

        public override void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
        {
            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                sizeCache.Invalidate();
            base.InvalidateFromChild(invalidation, source);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!sizeCache.IsValid)
            {
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
                width = Direction == Direction.Horizontal ? ItemsContainer.Width : width;
                height = Direction == Direction.Vertical ? ItemsContainer.Height : height;

                width = Math.Min(MaxWidth, width);
                height = Math.Min(MaxHeight, height);

                // Regardless of the above result, if we are relative-sizing, just use the stored width/height
                width = (RelativeSizeAxes & Axes.X) > 0 ? Width : width;
                height = (RelativeSizeAxes & Axes.Y) > 0 ? Height : height;

                if (State == MenuState.Closed && Direction == Direction.Horizontal)
                    width = 0;
                if (State == MenuState.Closed && Direction == Direction.Vertical)
                    height = 0;

                UpdateSize(new Vector2(width, height));

                sizeCache.Validate();
            }
        }

        /// <summary>
        /// Resizes this <see cref="Menu"/>.
        /// </summary>
        /// <param name="newSize">The new size.</param>
        protected virtual void UpdateSize(Vector2 newSize) => Size = newSize;

        #region Hover/Focus logic
        private void menuItemClicked(DrawableMenuItem item)
        {
            // We only want to close the sub-menu if we're not a sub menu - if we are a sub menu
            // then clicks should instead cause the sub menus to instantly show up
            if (TopLevelMenu && submenu?.State == MenuState.Open)
            {
                submenu.Close();
                return;
            }

            // Check if there is a sub menu to display
            if (item.Item.Items?.Count == 0)
            {
                // This item must have attempted to invoke an action - close all menus
                closeAll();
                return;
            }

            openDelegate?.Cancel();

            openSubmenuFor(item);
        }

        private DrawableMenuItem selectedItem;

        /// <summary>
        /// The item which triggered opening us as a submenu.
        /// </summary>
        private MenuItem triggeringItem;

        private void openSubmenuFor(DrawableMenuItem item)
        {
            item.State = MenuItemState.Selected;

            if (submenu == null)
            {
                submenuContainer.Add(submenu = CreateSubMenu());
                submenu.parentMenu = this;
                submenu.StateChanged += submenuStateChanged;
            }

            submenu.triggeringItem = item.Item;

            submenu.Items = item.Item.Items;
            submenu.Position = item.ToSpaceOfOtherDrawable(new Vector2(
                Direction == Direction.Vertical ? item.DrawWidth : 0,
                Direction == Direction.Horizontal ? item.DrawHeight : 0), this);

            if (item.Item.Items.Count > 0)
            {
                if (submenu.State == MenuState.Open)
                    Schedule(delegate { GetContainingInputManager().ChangeFocus(submenu); });
                else
                    submenu.Open();
            }
            else
                submenu.Close();
        }

        private void submenuStateChanged(MenuState state)
        {
            switch (state)
            {
                case MenuState.Closed:
                    selectedItem.State = MenuItemState.NotSelected;
                    break;
                case MenuState.Open:
                    selectedItem.State = MenuItemState.Selected;
                    break;
            }
        }

        private ScheduledDelegate openDelegate;
        private void menuItemHovered(DrawableMenuItem item)
        {
            // If we're not a sub-menu, then hover shouldn't display a sub-menu unless an item is clicked
            if (TopLevelMenu && submenu?.State != MenuState.Open)
                return;

            openDelegate?.Cancel();

            if (TopLevelMenu || HoverOpenDelay == 0)
                openSubmenuFor(item);
            else
            {
                openDelegate = Scheduler.AddDelayed(() =>
                {
                    if (item.IsHovered)
                        openSubmenuFor(item);
                }, HoverOpenDelay);
            }
        }

        protected override bool OnKeyDown(InputState state, KeyDownEventArgs args)
        {
            if (args.Key == Key.Escape && !TopLevelMenu)
            {
                Close();
                return true;
            }

            return base.OnKeyDown(state, args);
        }

        protected override bool OnClick(InputState state) => true;
        protected override bool OnHover(InputState state) => true;

        public override bool AcceptsFocus => !TopLevelMenu;

        public override bool RequestsFocus => !TopLevelMenu && State == MenuState.Open;

        protected override void OnFocusLost(InputState state)
        {
            // Case where a sub-menu was opened the focus will be transferred to that sub-menu while this menu will receive OnFocusLost
            if (submenu?.State == MenuState.Open)
                return;

            if (!TopLevelMenu)
                // At this point we should have lost focus due to clicks outside the menu structure
                closeAll();
        }

        /// <summary>
        /// Closes all open <see cref="Menu"/>s.
        /// </summary>
        private void closeAll()
        {
            Close();
            parentMenu?.closeFromChild(triggeringItem);
        }

        private void closeFromChild(MenuItem source)
        {
            if (IsHovered || (parentMenu?.IsHovered ?? false)) return;

            if (triggeringItem?.Items?.Contains(source) ?? false)
            {
                Close();
                parentMenu?.closeFromChild(triggeringItem);
            }
        }

        #endregion

        /// <summary>
        /// Creates a sub-menu for <see cref="MenuItem.Items"/> of <see cref="MenuItem"/>s added to this <see cref="Menu"/>.
        /// </summary>
        /// <returns></returns>
        protected virtual Menu CreateSubMenu() => new Menu(Direction.Vertical)
        {
            Anchor = Direction == Direction.Horizontal ? Anchor.BottomLeft : Anchor.TopRight
        };

        /// <summary>
        /// Creates the visual representation for a <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> that is to be visualised.</param>
        /// <returns>The visual representation.</returns>
        protected virtual DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableMenuItem(item);

        #region DrawableMenuItem
        // must be public due to mono bug(?) https://github.com/ppy/osu/issues/1204
        public class DrawableMenuItem : CompositeDrawable, IStateful<MenuItemState>
        {
            /// <summary>
            /// Invoked when this <see cref="DrawableMenuItem"/>'s <see cref="State"/> changes.
            /// </summary>
            public event Action<MenuItemState> StateChanged;

            /// <summary>
            /// Invoked when this <see cref="DrawableMenuItem"/> is clicked. This occurs regardless of whether or not <see cref="MenuItem.Action"/> was
            /// invoked or not, or whether <see cref="Item"/> contains any sub-<see cref="MenuItem"/>s.
            /// </summary>
            internal Action<DrawableMenuItem> Clicked;

            /// <summary>
            /// Invoked when this <see cref="DrawableMenuItem"/> is hovered. This runs one update frame behind the actual hover event.
            /// </summary>
            internal Action<DrawableMenuItem> Hovered;

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
            protected readonly Drawable Background;

            /// <summary>
            /// The foreground of this <see cref="DrawableMenuItem"/>. This contains the content of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            protected readonly Container Foreground;

            public DrawableMenuItem(MenuItem item)
            {
                Item = item;

                InternalChildren = new[]
                {
                    Background = CreateBackground(),
                    Foreground = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Child = Content = CreateContent()
                    },
                };

                if (Content is IHasText textContent)
                {
                    textContent.Text = item.Text;
                    Item.Text.ValueChanged += newText => textContent.Text = newText;
                }
            }

            /// <summary>
            /// Sets various properties of this <see cref="DrawableMenuItem"/> that depend on the direction in which
            /// <see cref="DrawableMenuItem"/>s flow inside the containing <see cref="Menu"/> (e.g. sizing axes).
            /// </summary>
            /// <param name="direction">The direction in which <see cref="DrawableMenuItem"/>s will be flowed.</param>
            public virtual void SetFlowDirection(Direction direction)
            {
                RelativeSizeAxes = direction == Direction.Horizontal ? Axes.Y : Axes.X;
                AutoSizeAxes = direction == Direction.Horizontal ? Axes.X : Axes.Y;
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

            private MenuItemState state;
            public MenuItemState State
            {
                get { return state; }
                set
                {
                    state = value;

                    UpdateForegroundColour();
                    UpdateBackgroundColour();

                    StateChanged?.Invoke(state);
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

                return false;
            }

            protected override void OnHoverLost(InputState state)
            {
                UpdateBackgroundColour();
                UpdateForegroundColour();
                base.OnHoverLost(state);
            }

            private bool hasSubmenu => Item.Items?.Count > 0;

            protected override bool OnClick(InputState state)
            {
                if (Item.Action.Disabled)
                    return true;

                if (!hasSubmenu)
                    Item.Action.Value?.Invoke();

                Clicked?.Invoke(this);

                return true;
            }

            /// <summary>
            /// Creates the background of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            protected virtual Drawable CreateBackground() => new Box { RelativeSizeAxes = Axes.Both };

            /// <summary>
            /// Creates the content which will be displayed in this <see cref="DrawableMenuItem"/>.
            /// If the <see cref="Drawable"/> returned implements <see cref="IHasText"/>, the text will be automatically
            /// updated when the <see cref="MenuItem.Text"/> is updated.
            /// </summary>
            protected virtual Drawable CreateContent() => new SpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Padding = new MarginPadding(5),
                TextSize = 17,
            };
        }
        #endregion
    }

    public enum MenuState
    {
        Closed,
        Open
    }

    public enum MenuItemState
    {
        NotSelected,
        Selected
    }
}
