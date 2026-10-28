// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DropdownSearchBar : VisibilityContainer, IFocusManager
    {
        public Bindable<string> SearchTerm { get; } = new Bindable<string>();

        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private IDropdown dropdown { get; set; } = null!;

        private TextBox textBox = null!;
        private DropdownTextInputSource? inputSource;

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
                })
            };

            dropdown.MenuStateChanged += onMenuStateChanged;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            inputSource = new DropdownTextInputSource(parent.Get<TextInputSource>(), parent.Get<GameHost>());

            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs(typeof(TextInputSource), inputSource);
            return dependencies;
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
            bool showTextBox = !host.OnScreenKeyboardOverlapsGameWindow && (AlwaysDisplayOnFocus || !string.IsNullOrEmpty(SearchTerm.Value));
            State.Value = textBox.HasFocus && showTextBox ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Handles textbox commits to select the current item.
        /// </summary>
        private void onTextBoxCommit(TextBox sender, bool newText)
        {
            dropdown.CommitPreselection();
            dropdown.CloseMenu();
        }

        /// <summary>
        /// Handles changes to the menu visibility.
        /// </summary>
        private void onMenuStateChanged(MenuState state)
        {
            if (state == MenuState.Closed)
            {
                // Reset states when the menu is closed by any means.
                SearchTerm.Value = string.Empty;

                if (textBox.HasFocus)
                    dropdown.ChangeFocus(null);

                dropdown.CloseMenu();
            }
            else
                dropdown.ChangeFocus(textBox);

            updateTextBoxVisibility();
        }

        /// <summary>
        /// Creates the <see cref="TextBox"/>.
        /// </summary>
        protected abstract TextBox CreateTextBox();

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

        private class DropdownTextInputSource : TextInputSource
        {
            private bool allowTextInput => !host.OnScreenKeyboardOverlapsGameWindow;

            private readonly TextInputSource platformSource;
            private readonly GameHost host;
            private RectangleF? imeRectangle;

            public DropdownTextInputSource(TextInputSource platformSource, GameHost host)
            {
                this.platformSource = platformSource;
                this.host = host;

                platformSource.OnTextInput += TriggerTextInput;
                platformSource.OnImeComposition += TriggerImeComposition;
                platformSource.OnImeResult += TriggerImeResult;
            }

            protected override void ActivateTextInput(TextInputProperties properties)
            {
                base.ActivateTextInput(properties);

                if (allowTextInput)
                    platformSource.Activate(properties, imeRectangle ?? RectangleF.Empty);
            }

            protected override void EnsureTextInputActivated(TextInputProperties properties)
            {
                base.EnsureTextInputActivated(properties);

                if (allowTextInput)
                    platformSource.EnsureActivated(properties, imeRectangle);
            }

            protected override void DeactivateTextInput()
            {
                base.DeactivateTextInput();

                imeRectangle = null;

                if (allowTextInput)
                    platformSource.Deactivate();
            }

            public override void SetImeRectangle(RectangleF rectangle)
            {
                base.SetImeRectangle(rectangle);

                imeRectangle = rectangle;

                if (allowTextInput)
                    platformSource.SetImeRectangle(rectangle);
            }

            public override void ResetIme()
            {
                base.ResetIme();

                if (allowTextInput)
                    platformSource.ResetIme();
            }
        }
    }
}
