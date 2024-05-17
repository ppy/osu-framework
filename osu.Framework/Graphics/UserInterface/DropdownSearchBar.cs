// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
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

            InternalChild = textBox = CreateTextBox().With(t =>
            {
                t.ReleaseFocusOnCommit = false;
                t.RelativeSizeAxes = Axes.Both;
                t.Size = new Vector2(1f);
                t.Current = SearchTerm;
            });
        }

        // Override to remove the visibility check. This element is always present even though it may not be physically visible on the screen.
        public override bool PropagatePositionalInputSubTree => IsPresent && RequestsPositionalInputSubTree && !IsMaskedAway;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            SearchTerm.BindValueChanged(v => updateVisibility());
            updateVisibility();
        }

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

            if (!hasFocus)
                SearchTerm.Value = string.Empty;

            updateVisibility();
            dropdown.ToggleMenu();
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

        protected abstract TextBox CreateTextBox();

        Drawable IFocusManager.FocusedDrawable => GetContainingFocusManager().FocusedDrawable;

        void IFocusManager.TriggerFocusContention(Drawable? triggerSource) => dropdown.TriggerFocusContention(triggerSource);

        public bool ChangeFocus(Drawable? potentialFocusTarget) => dropdown.ChangeFocus(potentialFocusTarget);
    }
}
