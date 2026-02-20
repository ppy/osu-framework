// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK.Graphics;
using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DropdownHeader : Container, IKeyBindingHandler<PlatformAction>
    {
        public event Action<DropdownSelectionAction> ChangeSelection;

        protected Container Background;
        protected Container Foreground;

        public bool AlwaysShowSearchBar
        {
            get => SearchBar.AlwaysDisplayOnFocus;
            set => SearchBar.AlwaysDisplayOnFocus = value;
        }

        /// <summary>
        /// Whether parent dropdown <see cref="Dropdown{T}"/> should open/close on OnMouseDown event.
        ///
        /// If not explicitly set, the value will be resolved to <c>true</c>
        /// if <see cref="IScrollContainer"/> is <b>not</b> found in the parent tree.
        /// </summary>
        public bool ToggleOnMouseDown
        {
            get => toggleOnMouseDownOverride ?? resolvedToggleOnMouseDown;
            set => toggleOnMouseDownOverride = value;
        }

        private bool? toggleOnMouseDownOverride;

        private bool resolvedToggleOnMouseDown;

        protected internal DropdownSearchBar SearchBar { get; }

        public Bindable<string> SearchTerm => SearchBar.SearchTerm;

        private Color4 backgroundColour = Color4.DarkGray;

        protected Color4 BackgroundColour
        {
            get => backgroundColour;
            set
            {
                backgroundColour = value;
                updateState();
            }
        }

        private Color4 disabledColour = Color4.Gray;

        protected Color4 DisabledColour
        {
            get => disabledColour;
            set
            {
                disabledColour = value;
                updateState();
            }
        }

        protected Color4 BackgroundColourHover { get; set; } = Color4.Gray;

        protected override Container<Drawable> Content => Foreground;

        protected internal abstract LocalisableString Label { get; set; }

        public readonly IBindable<bool> Enabled = new Bindable<bool>(true);

        [Resolved]
        private IDropdown dropdown { get; set; } = null!;

        protected DropdownHeader()
        {
            Masking = true;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Width = 1;

            InternalChildren = new Drawable[]
            {
                Background = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.DarkGray,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                    },
                },
                Foreground = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
                SearchBar = CreateSearchBar(),
                new UIEventHandler
                {
                    RelativeSizeAxes = Axes.Both,
                    UIEventHandle = handleUIEvent
                },
            };
        }

        protected abstract DropdownSearchBar CreateSearchBar();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Make dropdown toggleable on MouseDown event when inside a non-scrollable container
            if (toggleOnMouseDownOverride == null)
                resolvedToggleOnMouseDown = this.FindClosestParent<IScrollContainer>() == null;

            Enabled.BindTo(dropdown.Enabled);
            Enabled.BindValueChanged(_ => updateState(), true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateState();
            base.OnHoverLost(e);
        }

        private void updateState()
        {
            Colour = Enabled.Value ? Color4.White : DisabledColour;
            Background.Colour = IsHovered && Enabled.Value ? BackgroundColourHover : BackgroundColour;
        }

        /// <summary>
        /// Handles clicks and mouse events on the header to open/close the menu.
        /// </summary>
        private bool handleUIEvent(UIEvent e)
        {
            // Allow input to fall through to the search bar (and its contained textbox) if there's any search text.
            if (SearchBar.State.Value == Visibility.Visible && !string.IsNullOrEmpty(SearchTerm.Value))
                return false;

            switch (e)
            {
                case MouseDownEvent mouseDown:
                    return onMouseDown(mouseDown);

                case ClickEvent click:
                    return onClick(click);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Handles clicks on the header to open/close the menu.
        /// </summary>
        private bool onClick(ClickEvent e)
        {
            // No need to handle dropdown as with this flag it has already been toggled by `onMouseDown` handler
            if (ToggleOnMouseDown)
            {
                // UIEventHandler grows in the parent container, so there might be a situation
                // when dropdown is opened by clicking outside `SearchBar.textBox`,
                // which will lose focus and, therefore, close dropdown.
                // To prevent that, restore focus manually.
                if (dropdown.MenuState == MenuState.Open)
                    SearchBar.ObtainFocus();

                return false;
            }

            // Otherwise, the header acts as a button to show/hide the menu.
            dropdown.ToggleMenu();
            return true;
        }

        /// <summary>
        /// Handles mouse presses on the header to open/close the menu.
        /// </summary>
        private bool onMouseDown(MouseDownEvent e)
        {
            // Only proceed with the flag
            if (!ToggleOnMouseDown)
                return false;

            // Only allow dropdown to toggle when pressing primary mouse button
            if (e.Button != MouseButton.Left)
                return false;

            // Otherwise, the header acts as a button to show/hide the menu.
            dropdown.ToggleMenu();

            // And importantly, when the menu is closed as a result of the above toggle, block the search bar from receiving input.
            return dropdown.MenuState == MenuState.Closed;
        }

        public override bool HandleNonPositionalInput => IsHovered;

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!Enabled.Value)
                return false;

            switch (e.Key)
            {
                case Key.Up:
                    ChangeSelection?.Invoke(DropdownSelectionAction.Previous);
                    return true;

                case Key.Down:
                    ChangeSelection?.Invoke(DropdownSelectionAction.Next);
                    return true;

                default:
                    return base.OnKeyDown(e);
            }
        }

        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (!Enabled.Value)
                return false;

            switch (e.Action)
            {
                case PlatformAction.MoveToListStart:
                    ChangeSelection?.Invoke(DropdownSelectionAction.First);
                    return true;

                case PlatformAction.MoveToListEnd:
                    ChangeSelection?.Invoke(DropdownSelectionAction.Last);
                    return true;

                default:
                    return false;
            }
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        public enum DropdownSelectionAction
        {
            Previous,
            Next,
            First,
            Last,
            FirstVisible,
            LastVisible
        }

        private partial class UIEventHandler : Drawable
        {
            public required Func<UIEvent, bool> UIEventHandle { get; init; }

            protected override bool Handle(UIEvent e) => UIEventHandle(e);
        }
    }
}
