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
                    AutoSizeAxes = Axes.Y
                },
                SearchBar = CreateSearchBar(),
                new ClickHandler
                {
                    RelativeSizeAxes = Axes.Both,
                    Click = onClick
                }
            };
        }

        protected abstract DropdownSearchBar CreateSearchBar();

        protected override void LoadComplete()
        {
            base.LoadComplete();

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
        /// Handles clicks on the header to open/close the menu.
        /// </summary>
        private bool onClick(ClickEvent e)
        {
            // Allow input to fall through to the search bar (and its contained textbox) if there's any search text.
            if (SearchBar.State.Value == Visibility.Visible && !string.IsNullOrEmpty(SearchTerm.Value))
                return false;

            // Otherwise, the header acts as a button to show/hide the menu.
            dropdown.ToggleMenu();
            return true;
        }

        public override bool HandleNonPositionalInput => IsHovered;

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!Enabled.Value)
                return true;

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
                return true;

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

        private partial class ClickHandler : Drawable
        {
            public required Func<ClickEvent, bool> Click { get; init; }
            protected override bool OnClick(ClickEvent e) => Click(e);
        }
    }
}
