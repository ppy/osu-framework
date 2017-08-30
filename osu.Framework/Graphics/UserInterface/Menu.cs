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
using OpenTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class Menu : CompositeDrawable, IStateful<MenuState>
    {
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

        private readonly Box background;

        private readonly Direction direction;

        public Menu(Direction direction)
        {
            this.direction = direction;

            Masking = true;

            InternalChildren = new Drawable[]
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

        public bool ScrollbarVisible
        {
            get { return ContentContainer.ScrollbarVisible; }
            set { ContentContainer.ScrollbarVisible = value; }
        }

        private float maxWidth = float.MaxValue;
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

        private MenuState state = MenuState.Closed;
        /// <summary>
        /// Gets or sets the current state of this <see cref="Menu"/>.
        /// </summary>
        public virtual MenuState State
        {
            get { return state; }
            set
            {
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

                    if (HasFocus)
                        GetContainingInputManager().ChangeFocus(null);
                    break;
                case MenuState.Opened:
                    AnimateOpen();

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
        public void Add(MenuItem item)
        {
            var drawableItem = CreateDrawableMenuItem(item);
            drawableItem.AutoSizeAxes = ItemsContainer.AutoSizeAxes;
            drawableItem.RelativeSizeAxes = ItemsContainer.RelativeSizeAxes;
            drawableItem.CloseRequested = Close;

            ItemsContainer.Add(drawableItem);
        }

        /// <summary>
        /// Removes a <see cref="MenuItem"/> from this <see cref="Menu"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(MenuItem item) => ItemsContainer.RemoveAll(r => r.Item == item) > 0;

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

        public override bool AcceptsFocus => true;
        protected override bool OnClick(InputState state) => true;
        protected override void OnFocusLost(InputState state) => State = MenuState.Closed;

        /// <summary>
        /// Creates the visual representation for a <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> that is to be visualised.</param>
        /// <returns>The visual representation.</returns>
        protected virtual DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableMenuItem(item);

        #region DrawableMenuItem
        protected class DrawableMenuItem : CompositeDrawable
        {
            public readonly MenuItem Item;

            /// <summary>
            /// Fired generally when this item was clicked and requests the containing menu to close itself.
            /// </summary>
            public Action CloseRequested;

            protected readonly Drawable Content;

            protected readonly Box Background;
            protected readonly Container Foreground;

            public DrawableMenuItem(MenuItem item)
            {
                Item = item;

                InternalChildren = new Drawable[]
                {
                    Background = new Box { RelativeSizeAxes = Axes.Both },
                    Foreground = new Container { Child = Content = CreateContent() }
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
