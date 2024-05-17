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

        private bool alwaysDisplayOnFocus;

        public bool AlwaysDisplayOnFocus
        {
            get => alwaysDisplayOnFocus;
            set
            {
                alwaysDisplayOnFocus = value;

                if (IsLoaded)
                    updateTextBoxVisibility();
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
                    t.OnCommit += onTextBoxCommit;
                }),
                new ClickHandler
                {
                    RelativeSizeAxes = Axes.Both,
                    Click = onClick
                }
            };

            dropdown.MenuStateChanged += onMenuStateChanged;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            SearchTerm.BindValueChanged(_ => updateTextBoxVisibility(), true);
        }

        public override bool PropagateNonPositionalInputSubTree => dropdown.Enabled.Value && base.PropagateNonPositionalInputSubTree;

        // Importantly, this also removes the visibility condition of the base implementation - this element is always present even though it may not be physically visible on the screen.
        public override bool PropagatePositionalInputSubTree => dropdown.Enabled.Value && RequestsPositionalInputSubTree && !IsMaskedAway;

        protected override void Update()
        {
            base.Update();

            updateMenuState();
            updateTextBoxVisibility();
        }

        /// <summary>
        /// Clears the search term.
        /// </summary>
        /// <returns>If the search term was cleared.</returns>
        public bool Back()
        {
            if (string.IsNullOrEmpty(SearchTerm.Value))
                return false;

            SearchTerm.Value = string.Empty;
            return true;
        }

        /// <summary>
        /// Opens or closes the menu depending on whether the textbox is focused.
        /// </summary>
        private void updateMenuState()
        {
            if (textBox.HasFocus)
                dropdown.OpenMenu();
            else
                dropdown.CloseMenu();
        }

        /// <summary>
        /// Updates the textbox visibility.
        /// </summary>
        private void updateTextBoxVisibility()
        {
            bool showTextBox = AlwaysDisplayOnFocus || !string.IsNullOrEmpty(SearchTerm.Value);
            State.Value = textBox.HasFocus && showTextBox ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Handles textbox commits to select the current item.
        /// </summary>
        private void onTextBoxCommit(TextBox sender, bool newText) => dropdown.CommitPreselection();

        /// <summary>
        /// Handles changes to the menu visibility.
        /// </summary>
        private void onMenuStateChanged(MenuState state)
        {
            // Reset states when the menu is closed by any means.
            if (state == MenuState.Closed)
            {
                SearchTerm.Value = string.Empty;

                if (textBox.HasFocus)
                    dropdown.ChangeFocus(null);

                dropdown.CloseMenu();
            }
        }

        /// <summary>
        /// Handles clicks on the search bar.
        /// </summary>
        private bool onClick(ClickEvent e)
        {
            // Allow input to fall through to the textbox if it's visible.
            if (State.Value == Visibility.Visible)
                return false;

            // Otherwise, the search box acts as a button to show/hide the menu.
            dropdown.ToggleMenu();

            // And importantly, when the menu is closed as a result of the above toggle,
            // block the textbox from receiving input so that it doesn't get re-focused.
            return dropdown.MenuState == MenuState.Closed;
        }

        /// <summary>
        /// Creates the <see cref="TextBox"/>.
        /// </summary>
        protected abstract TextBox CreateTextBox();

        Drawable IFocusManager.FocusedDrawable => GetContainingFocusManager().FocusedDrawable;

        void IFocusManager.TriggerFocusContention(Drawable? triggerSource)
        {
            // Clear search text first without releasing focus.
            if (Back())
                return;

            dropdown.TriggerFocusContention(triggerSource);
        }

        bool IFocusManager.ChangeFocus(Drawable? potentialFocusTarget)
        {
            // Clear search text first without releasing focus.
            if (Back())
                return false;

            return dropdown.ChangeFocus(potentialFocusTarget);
        }

        private partial class ClickHandler : Drawable
        {
            public required Func<ClickEvent, bool> Click { get; init; }
            protected override bool OnClick(ClickEvent e) => Click(e);
        }
    }
}
