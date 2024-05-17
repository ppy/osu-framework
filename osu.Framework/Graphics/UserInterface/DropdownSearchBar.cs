// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DropdownSearchBar : VisibilityContainer, IFocusManager
    {
        public Bindable<string> SearchTerm { get; } = new Bindable<string>();

        [Resolved]
        private IDropdown dropdown { get; set; } = null!;

        private TextBox textBox = null!;
        private bool hasFocus;

        private bool alwaysDisplayOnFocus;

        public bool AlwaysDisplayOnFocus
        {
            get => alwaysDisplayOnFocus;
            set
            {
                alwaysDisplayOnFocus = value;

                if (IsLoaded)
                    updateVisibility();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            AlwaysPresent = true;

            InternalChildren = new Drawable[]
            {
                textBox = CreateTextBox().With(t =>
                {
                    t.RelativeSizeAxes = Axes.Both;
                    t.Size = new Vector2(1f);
                    t.Current = SearchTerm;
                    t.ReleaseFocusOnCommit = true;
                    t.CommitOnFocusLost = false;
                    t.OnCommit += (_, _) => dropdown.CommitPreselection();
                }),
                new ClickHandler
                {
                    RelativeSizeAxes = Axes.Both,
                    Click = onClick
                }
            };

            dropdown.MenuStateChanged += state =>
            {
                if (state == MenuState.Closed && textBox.HasFocus)
                    dropdown.ChangeFocus(null);
            };

            dropdown.Enabled.BindValueChanged(enabled =>
            {
                if (!enabled.NewValue && textBox.HasFocus)
                    dropdown.ChangeFocus(null);
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            SearchTerm.BindValueChanged(v => updateVisibility());
            updateVisibility();
        }

        public override bool PropagateNonPositionalInputSubTree => dropdown.Enabled.Value && base.PropagateNonPositionalInputSubTree;

        // Importantly, this also removes the visibility condition of the base implementation - this element is always present even though it may not be physically visible on the screen.
        public override bool PropagatePositionalInputSubTree => dropdown.Enabled.Value && RequestsPositionalInputSubTree && !IsMaskedAway;

        protected override void Update()
        {
            base.Update();
            updateFocus();
        }

        private void updateFocus()
        {
            if (hasFocus == textBox.HasFocus)
                return;

            hasFocus = textBox.HasFocus;

            if (hasFocus)
                dropdown.ShowMenu();
            else
                dropdown.HideMenu();

            if (!hasFocus)
                SearchTerm.Value = string.Empty;

            updateVisibility();
        }

        public bool Back()
        {
            if (!string.IsNullOrEmpty(SearchTerm.Value))
            {
                SearchTerm.Value = string.Empty;
                return true;
            }

            return false;
        }

        private void updateVisibility()
        {
            bool showTextBox = AlwaysDisplayOnFocus || !string.IsNullOrEmpty(SearchTerm.Value);

            State.Value = hasFocus && showTextBox
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        private bool onClick(ClickEvent e)
        {
            // Always allow input to fall through if the textbox is visible.
            if (State.Value == Visibility.Visible)
                return false;

            // Otherwise, the search box acts as a hook to show/hide the menu.
            dropdown.ToggleMenu();

            // Importantly, when the menu is closed as a result of the above toggle,
            // block the textbox from receiving input so that it doesn't get re-focused.
            return dropdown.MenuState == MenuState.Closed;
        }

        protected abstract TextBox CreateTextBox();

        // Focus management is isolated to the IDropdown interface by Dropdown.
        Drawable IFocusManager.FocusedDrawable => GetContainingFocusManager().FocusedDrawable;
        void IFocusManager.TriggerFocusContention(Drawable? triggerSource) => dropdown.TriggerFocusContention(triggerSource);
        bool IFocusManager.ChangeFocus(Drawable? potentialFocusTarget) => dropdown.ChangeFocus(potentialFocusTarget);

        private partial class ClickHandler : Drawable
        {
            public required Func<ClickEvent, bool> Click { get; init; }

            protected override bool OnClick(ClickEvent e) => Click(e);
        }
    }
}
