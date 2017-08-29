// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A list of command or selection items.
    /// </summary>
    public class Menu : CompositeDrawable, IStateful<MenuState>
    {
        /// <summary>
        /// Invoked when this <see cref="Menu"/> has opened.
        /// </summary>
        public event Action OnOpen;

        /// <summary>
        /// Invoked when this <see cref="Menu"/> has closed. This may have been the cause of either a selection
        /// or a click outside of the <see cref="Menu"/> and does not indicate a selection has occurred.
        /// </summary>
        public event Action OnClose;

        /// <summary>
        /// Gets or sets the <see cref="MenuItem"/>s contained within this <see cref="Menu"/>.
        /// </summary>
        public IReadOnlyList<MenuItem> Items
        {
            get { return itemsContainer.Select(r => r.Item).ToList(); }
            set
            {
                itemsContainer.Clear();

                foreach (var item in value)
                    Add(item);

                menuWidth.Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets whether the scroll bar of this <see cref="Menu"/> is visible.
        /// </summary>
        public bool ScrollbarVisible
        {
            get { return scrollContainer.ScrollbarVisible; }
            set { scrollContainer.ScrollbarVisible = value; }
        }

        public new Axes RelativeSizeAxes
        {
            get { return base.RelativeSizeAxes; }
            set { throw new InvalidOperationException($"{nameof(MenuItem)} will determine its size based on the value of {nameof(UseParentWidth)}."); }
        }

        public new Axes AutoSizeAxes
        {
            get { return base.AutoSizeAxes; }
            set { throw new InvalidOperationException($"{nameof(MenuItem)} will determine its size based on the value of {nameof(UseParentWidth)}."); }
        }

        private bool useParentWidth;

        /// <summary>
        /// Gets or sets whether the menu should expand to the width of the parent. If false, a width will be calculated based on the widest item.
        /// </summary>
        public bool UseParentWidth
        {
            get { return useParentWidth; }
            set
            {
                useParentWidth = value;
                menuWidth.Invalidate();
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

        private Cached menuWidth = new Cached();

        private readonly Box background;
        private readonly ScrollContainer scrollContainer;
        private readonly FlowContainer<DrawableMenuItem> itemsContainer;

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
                    Child = itemsContainer = new FillFlowContainer<DrawableMenuItem>
                    {
                        Direction = FillDirection.Vertical,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = ItemFlowContainerPadding
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            updateState();
        }

        /// <summary>
        /// Adds a <see cref="MenuItem"/> to this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to add.</param>
        public void Add(MenuItem item)
        {
            var drawableItem = CreateDrawableMenuItem(item);
            drawableItem.CloseRequested = Close;

            itemsContainer.Add(drawableItem);
            menuWidth.Invalidate();
        }

        /// <summary>
        /// Removes a <see cref="MenuItem"/> from this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(MenuItem item) => itemsContainer.RemoveAll(r => r.Item == item) > 0;

        /// <summary>
        /// Clears all <see cref="MenuItem"/>s in this <see cref="Menu"/>.
        /// </summary>
        public void Clear() => itemsContainer.Clear();

        /// <summary>
        /// Gets the item representations contained by this <see cref="Menu"/>.
        /// </summary>
        protected IReadOnlyList<DrawableMenuItem> Children => itemsContainer;

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu"/>.
        /// </summary>
        public MenuState State
        {
            get { return state; }
            set
            {
                if (state == value)
                    return;
                state = value;

                if (!IsLoaded) return;

                updateState();
            }
        }

        private void updateState()
        {
            switch (state)
            {
                case MenuState.Closed:
                    AnimateClose();

                    if (HasFocus)
                        GetContainingInputManager().ChangeFocus(null);

                    OnClose?.Invoke();
                    break;
                case MenuState.Opened:
                    AnimateOpen();

                    //schedule required as we may not be present currently.
                    Schedule(() =>
                    {
                        if (State == MenuState.Opened)
                            GetContainingInputManager().ChangeFocus(this);
                    });

                    OnOpen?.Invoke();
                    break;
            }

            UpdateMenuHeight();
        }

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

        private float maxHeight = float.MaxValue;
        /// <summary>
        /// Gets or sets maximum height allowable by this <see cref="Menu"/>.
        /// </summary>
        public float MaxHeight
        {
            get { return maxHeight; }
            set
            {
                maxHeight = value;
                UpdateMenuHeight();
            }
        }

        /// <summary>
        /// Animates the opening of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void AnimateOpen() => Show();

        /// <summary>
        /// Animates the closing of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void AnimateClose() => Hide();

        public override bool AcceptsFocus => true;
        protected override bool OnClick(InputState state) => true;
        protected override void OnFocusLost(InputState state) => State = MenuState.Closed;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            UpdateMenuHeight();
            updateMenuWidth();
        }

        /// <summary>
        /// The height of the <see cref="MenuItem"/>s contained by this <see cref="Menu"/>, clamped by <see cref="MaxHeight"/>.
        /// </summary>
        protected float ContentHeight => Math.Min(itemsContainer.Height, MaxHeight);

        /// <summary>
        /// Computes and applies the height of this <see cref="Menu"/>.
        /// </summary>
        protected virtual void UpdateMenuHeight() => Height = ContentHeight;

        private void updateMenuWidth()
        {
            if (menuWidth.IsValid)
                return;

            if (UseParentWidth)
            {
                base.RelativeSizeAxes = Axes.X;
                Width = 1;
            }
            else
            {
                // We're now defining the size of ourselves based on our children, but our children are relatively-sized, so we need to compute our size ourselves
                float textWidth = 0;

                foreach (var item in Children)
                    textWidth = Math.Max(textWidth, item.TextDrawWidth);

                Width = textWidth;
            }

            menuWidth.Validate();
        }

        /// <summary>
        /// Creates the visual representation for a <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> that is to be visualised.</param>
        /// <returns>The visual representation.</returns>
        protected virtual DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableMenuItem(item);

        protected virtual MarginPadding ItemFlowContainerPadding => new MarginPadding();

        #region DrawableMenuItem
        protected class DrawableMenuItem : CompositeDrawable
        {
            public readonly MenuItem Item;

            /// <summary>
            /// Fired generally when this item was clicked and requests the containing menu to close itself.
            /// </summary>
            public Action CloseRequested;

            private readonly Drawable content;

            protected readonly Box Background;
            protected readonly Container Foreground;

            public DrawableMenuItem(MenuItem item)
            {
                Item = item;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                InternalChildren = new Drawable[]
                {
                    Background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    Foreground = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Child = content = CreateContent(),
                    },
                };

                var textContent = content as IHasText;
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

            /// <summary>
            /// The draw width of the text of this <see cref="DrawableMenuItem"/>.
            /// </summary>
            public float TextDrawWidth => content.DrawWidth;

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
                if (Item.Action.Disabled)
                    return false;

                Item.Action.Value?.Invoke();
                CloseRequested?.Invoke();
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
                TextSize = 17
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
