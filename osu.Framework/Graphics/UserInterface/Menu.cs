// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class Menu : CompositeDrawable, IStateful<MenuState>
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
        protected readonly ScrollContainer<Drawable> ContentContainer;

        /// <summary>
        /// The <see cref="Container{T}"/> that contains the items of this <see cref="Menu"/>.
        /// </summary>
        protected FillFlowContainer<DrawableMenuItem> ItemsContainer => itemsFlow;

        /// <summary>
        /// The container that provides the masking effects for this <see cref="Menu"/>.
        /// </summary>
        protected readonly Container MaskingContainer;

        /// <summary>
        /// Gets the item representations contained by this <see cref="Menu"/>.
        /// </summary>
        protected internal IReadOnlyList<DrawableMenuItem> Children => ItemsContainer.Children;

        protected readonly Direction Direction;

        private ItemsFlow itemsFlow;
        private Menu parentMenu;
        private Menu submenu;

        private readonly Box background;

        private readonly Container<Menu> submenuContainer;
        private readonly LayoutValue positionLayout = new LayoutValue(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit);

        /// <summary>
        /// Constructs a menu.
        /// </summary>
        /// <param name="direction">The direction of layout for this menu.</param>
        /// <param name="topLevelMenu">Whether the resultant menu is always displayed in an open state (ie. a menu bar).</param>
        protected Menu(Direction direction, bool topLevelMenu = false)
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
                        ContentContainer = CreateScrollContainer(direction).With(d =>
                        {
                            d.RelativeSizeAxes = Axes.Both;
                            d.Masking = false;
                            d.Child = itemsFlow = new ItemsFlow { Direction = direction == Direction.Horizontal ? FillDirection.Horizontal : FillDirection.Vertical };
                        })
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

            AddLayout(positionLayout);
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
            get => ItemsContainer.Select(r => r.Item).ToList();
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
            get => background.Colour;
            set => background.Colour = value;
        }

        /// <summary>
        /// Gets or sets whether the scroll bar of this <see cref="Menu"/> should be visible.
        /// </summary>
        public bool ScrollbarVisible
        {
            get => ContentContainer.ScrollbarVisible;
            set => ContentContainer.ScrollbarVisible = value;
        }

        private float maxWidth = float.MaxValue;

        /// <summary>
        /// Gets or sets the maximum allowable width by this <see cref="Menu"/>.
        /// </summary>
        public float MaxWidth
        {
            get => maxWidth;
            set
            {
                if (Precision.AlmostEquals(maxWidth, value))
                    return;

                maxWidth = value;

                itemsFlow.SizeCache.Invalidate();
            }
        }

        private float maxHeight = float.PositiveInfinity;

        /// <summary>
        /// Gets or sets the maximum allowable height by this <see cref="Menu"/>.
        /// </summary>
        public float MaxHeight
        {
            get => maxHeight;
            set
            {
                if (Precision.AlmostEquals(maxHeight, value))
                    return;

                maxHeight = value;

                itemsFlow.SizeCache.Invalidate();
            }
        }

        private MenuState state = MenuState.Closed;

        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu"/>.
        /// </summary>
        public virtual MenuState State
        {
            get => state;
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

            resetState();

            switch (State)
            {
                case MenuState.Closed:
                    AnimateClose();

                    if (HasFocus)
                        GetContainingInputManager()?.ChangeFocus(parentMenu);
                    break;

                case MenuState.Open:
                    ContentContainer.ScrollToStart(false);

                    AnimateOpen();

                    // We may not be present at this point, so must run on the next frame.
                    if (!TopLevelMenu)
                    {
                        Schedule(delegate
                        {
                            if (State == MenuState.Open) GetContainingInputManager().ChangeFocus(this);
                        });
                    }

                    break;
            }
        }

        private void resetState()
        {
            if (!IsLoaded)
                return;

            submenu?.Close();
            itemsFlow.SizeCache.Invalidate();
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
            itemsFlow.SizeCache.Invalidate();
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
            itemsFlow.SizeCache.Invalidate();

            return result;
        }

        /// <summary>
        /// Clears all <see cref="MenuItem"/>s in this <see cref="Menu"/>.
        /// </summary>
        public void Clear()
        {
            ItemsContainer.Clear();
            resetState();
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

        protected override void Update()
        {
            base.Update();

            if (!positionLayout.IsValid && State == MenuState.Open && parentMenu != null)
            {
                var inputManager = GetContainingInputManager();

                // This is the default position to which this menu should be anchored to the parent menu item which triggered it (top left of the triggering item)
                var triggeringItemTopLeftPosition = triggeringItem.ToSpaceOfOtherDrawable(Vector2.Zero, parentMenu);

                // The "maximum" position is the worst case position of the bottom right corner of this menu
                // if this menu is anchored top-left to the triggering item.
                var menuMaximumPosition = triggeringItem.ToSpaceOfOtherDrawable(
                    new Vector2(
                        triggeringItem.DrawWidth + DrawWidth,
                        triggeringItem.DrawHeight + DrawHeight), inputManager);

                // The "minimum" position is the worst case position of the top left corner of this menu
                // if this menu is anchored bottom-right to the parent menu item that triggered it.
                var menuMinimumPosition = triggeringItem.ToSpaceOfOtherDrawable(new Vector2(-DrawWidth, -DrawHeight), inputManager);

                // We will be making anchor adjustments by changing the parent's "submenu container" to be positioned and anchored correctly to the parent menu.
                // Therefore note that all X and Y adjustments below will occur in the parent menu's coordinates.
                var parentSubmenuContainer = parentMenu.submenuContainer;

                if (parentMenu.Direction == Direction.Vertical)
                {
                    // If this menu won't fit on the screen horizontally if it's anchored to the right of its triggering item, but it will fit when anchored to the left...
                    if (menuMaximumPosition.X > inputManager.DrawWidth && menuMinimumPosition.X > 0)
                    {
                        // switch the origin and position of the submenu container so that it's right-aligned to the left side of the triggering item.
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.x0, Anchor.x2);
                        parentSubmenuContainer.X = triggeringItemTopLeftPosition.X;
                    }
                    else
                    {
                        // otherwise, switch the origin and position of the submenu container so that it's left-aligned to the right side of the triggering item.
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.x2, Anchor.x0);
                        parentSubmenuContainer.X = triggeringItemTopLeftPosition.X + triggeringItem.DrawWidth;
                    }

                    // If this menu won't fit on the screen vertically if its top edge is aligned to the top of the triggering item,
                    // but it will fit if its bottom edge is aligned to the bottom of the triggering item...
                    if (menuMaximumPosition.Y > inputManager.DrawHeight && menuMinimumPosition.Y > 0)
                    {
                        // switch the origin and position of the submenu container so that it's bottom-aligned to the bottom of the triggering item.
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.y0, Anchor.y2);
                        parentSubmenuContainer.Y = triggeringItemTopLeftPosition.Y + triggeringItem.DrawHeight;
                    }
                    else
                    {
                        // otherwise, switch the origin and position of the submenu container so that it's top-aligned to the top of the triggering item.
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.y2, Anchor.y0);
                        parentSubmenuContainer.Y = triggeringItemTopLeftPosition.Y;
                    }
                }
                // the "horizontal" case is the same as above, but with the axes everywhere swapped.
                else
                {
                    if (menuMaximumPosition.Y > inputManager.DrawHeight && menuMinimumPosition.Y > 0)
                    {
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.y0, Anchor.y2);
                        parentSubmenuContainer.Y = triggeringItemTopLeftPosition.Y;
                    }
                    else
                    {
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.y2, Anchor.y0);
                        parentSubmenuContainer.Y = triggeringItemTopLeftPosition.Y + triggeringItem.DrawHeight;
                    }

                    if (menuMaximumPosition.X > inputManager.DrawWidth && menuMinimumPosition.X > 0)
                    {
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.x0, Anchor.x2);
                        parentSubmenuContainer.X = triggeringItemTopLeftPosition.X + triggeringItem.DrawWidth;
                    }
                    else
                    {
                        parentSubmenuContainer.Origin = switchAxisAnchors(parentSubmenuContainer.Origin, Anchor.x2, Anchor.x0);
                        parentSubmenuContainer.X = triggeringItemTopLeftPosition.X;
                    }
                }

                positionLayout.Validate();

                static Anchor switchAxisAnchors(Anchor originalValue, Anchor toDisable, Anchor toEnable) => (originalValue & ~toDisable) | toEnable;
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!itemsFlow.SizeCache.IsValid)
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
                width = RelativeSizeAxes.HasFlagFast(Axes.X) ? Width : width;
                height = RelativeSizeAxes.HasFlagFast(Axes.Y) ? Height : height;

                if (State == MenuState.Closed && Direction == Direction.Horizontal)
                    width = 0;
                if (State == MenuState.Closed && Direction == Direction.Vertical)
                    height = 0;

                UpdateSize(new Vector2(width, height));

                itemsFlow.SizeCache.Validate();
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
                // This item must have attempted to invoke an action - close all menus if item allows
                if (item.CloseMenuOnClick)
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
        private DrawableMenuItem triggeringItem;

        private void openSubmenuFor(DrawableMenuItem item)
        {
            item.State = MenuItemState.Selected;

            if (submenu == null)
            {
                submenuContainer.Add(submenu = CreateSubMenu());
                submenu.parentMenu = this;
                submenu.StateChanged += submenuStateChanged;
            }

            submenu.triggeringItem = item;
            submenu.positionLayout.Invalidate();

            submenu.Items = item.Item.Items;

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

        public override bool HandleNonPositionalInput => State == MenuState.Open;

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape && !TopLevelMenu)
            {
                Close();
                return true;
            }

            return base.OnKeyDown(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;
        protected override bool OnClick(ClickEvent e) => true;
        protected override bool OnHover(HoverEvent e) => true;

        public override bool AcceptsFocus => !TopLevelMenu;

        public override bool RequestsFocus => !TopLevelMenu && State == MenuState.Open;

        protected override void OnFocusLost(FocusLostEvent e)
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
            parentMenu?.closeFromChild(triggeringItem.Item);
        }

        private void closeFromChild(MenuItem source)
        {
            if (IsHovered || (parentMenu?.IsHovered ?? false)) return;

            if (triggeringItem?.Item.Items?.Contains(source) ?? triggeringItem == null)
            {
                Close();
                parentMenu?.closeFromChild(triggeringItem.Item);
            }
        }

        #endregion

        /// <summary>
        /// Creates a sub-menu for <see cref="MenuItem.Items"/> of <see cref="MenuItem"/>s added to this <see cref="Menu"/>.
        /// </summary>
        protected abstract Menu CreateSubMenu();

        /// <summary>
        /// Creates the visual representation for a <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> that is to be visualised.</param>
        /// <returns>The visual representation.</returns>
        protected abstract DrawableMenuItem CreateDrawableMenuItem(MenuItem item);

        /// <summary>
        /// Creates the <see cref="ScrollContainer{T}"/> to hold the items of this <see cref="Menu"/>.
        /// </summary>
        /// <param name="direction">The scrolling direction.</param>
        /// <returns>The <see cref="ScrollContainer{T}"/>.</returns>
        protected abstract ScrollContainer<Drawable> CreateScrollContainer(Direction direction);

        #region DrawableMenuItem

        // must be public due to mono bug(?) https://github.com/ppy/osu/issues/1204
        public abstract class DrawableMenuItem : CompositeDrawable, IStateful<MenuItemState>
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

            /// <summary>
            /// Whether to close all menus when this action <see cref="DrawableMenuItem"/> is clicked.
            /// </summary>
            public virtual bool CloseMenuOnClick => true;

            protected DrawableMenuItem(MenuItem item)
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
                    textContent.Text = item.Text.Value;
                    Item.Text.ValueChanged += e => textContent.Text = e.NewValue;
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
                get => backgroundColour;
                set
                {
                    backgroundColour = value;
                    Scheduler.AddOnce(UpdateBackgroundColour);
                }
            }

            private Color4 foregroundColour = Color4.White;

            /// <summary>
            /// Gets or sets the default foreground colour.
            /// </summary>
            public Color4 ForegroundColour
            {
                get => foregroundColour;
                set
                {
                    foregroundColour = value;
                    Scheduler.AddOnce(UpdateForegroundColour);
                }
            }

            private Color4 backgroundColourHover = Color4.DarkGray;

            /// <summary>
            /// Gets or sets the background colour when this <see cref="DrawableMenuItem"/> is hovered.
            /// </summary>
            public Color4 BackgroundColourHover
            {
                get => backgroundColourHover;
                set
                {
                    backgroundColourHover = value;
                    Scheduler.AddOnce(UpdateBackgroundColour);
                }
            }

            private Color4 foregroundColourHover = Color4.White;

            /// <summary>
            /// Gets or sets the foreground colour when this <see cref="DrawableMenuItem"/> is hovered.
            /// </summary>
            public Color4 ForegroundColourHover
            {
                get => foregroundColourHover;
                set
                {
                    foregroundColourHover = value;
                    Scheduler.AddOnce(UpdateForegroundColour);
                }
            }

            private MenuItemState state;

            public MenuItemState State
            {
                get => state;
                set
                {
                    state = value;

                    Scheduler.AddOnce(UpdateBackgroundColour);
                    Scheduler.AddOnce(UpdateForegroundColour);

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

                Scheduler.AddOnce(UpdateBackgroundColour);
                Scheduler.AddOnce(UpdateForegroundColour);
            }

            protected override bool OnHover(HoverEvent e)
            {
                Scheduler.AddOnce(UpdateBackgroundColour);
                Scheduler.AddOnce(UpdateForegroundColour);

                Schedule(() =>
                {
                    if (IsHovered)
                        Hovered?.Invoke(this);
                });

                return false;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                Scheduler.AddOnce(UpdateBackgroundColour);
                Scheduler.AddOnce(UpdateForegroundColour);
                base.OnHoverLost(e);
            }

            private bool hasSubmenu => Item.Items?.Count > 0;

            protected override bool OnClick(ClickEvent e)
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
            protected abstract Drawable CreateContent();
        }

        #endregion

        private class ItemsFlow : FillFlowContainer<DrawableMenuItem>
        {
            public readonly LayoutValue SizeCache = new LayoutValue(Invalidation.RequiredParentSizeToFit, InvalidationSource.Self);

            public ItemsFlow()
            {
                AddLayout(SizeCache);
            }
        }
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
