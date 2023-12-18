// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DropdownSearchBar : VisibilityContainer
    {
        [Resolved]
        private GameHost? host { get; set; }

        private TextBox textBox = null!;
        private PassThroughInputManager textBoxInputManager = null!;

        public Bindable<string> SearchTerm { get; } = new Bindable<string>();

        // handling mouse input on dropdown header is not easy, since the menu would lose focus on release and automatically close
        public override bool HandlePositionalInput => false;
        public override bool PropagatePositionalInputSubTree => false;

        private bool obtainedFocus;

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
            AlwaysPresent = true;
            RelativeSizeAxes = Axes.Both;

            // Dropdown menus rely on their focus state to determine when they should be closed.
            // On the other hand, text boxes require to be focused in order for the user to interact with them.
            // To handle that matter, we'll wrap the search text box inside a local input manager, and manage its focus state accordingly.
            InternalChild = textBoxInputManager = new PassThroughInputManager
            {
                RelativeSizeAxes = Axes.Both,
                Child = textBox = CreateTextBox().With(t =>
                {
                    t.ReleaseFocusOnCommit = false;
                    t.RelativeSizeAxes = Axes.Both;
                    t.Size = new Vector2(1f);
                    t.Current = SearchTerm;
                })
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            SearchTerm.BindValueChanged(v => updateVisibility());
            updateVisibility();
        }

        public void ObtainFocus()
        {
            // On mobile platforms, let's not make the keyboard popup unless the dropdown is intentionally searchable.
            // Unfortunately it is not enough to just early-return here,
            // as even despite that the text box will receive focus via the text box input manager;
            // it is necessary to cut off the text box input manager from parent input entirely.
            // TODO: preferably figure out a better way to do this.
            bool willShowOverlappingKeyboard = host?.OnScreenKeyboardOverlapsGameWindow == true;

            if (willShowOverlappingKeyboard && !AlwaysDisplayOnFocus)
            {
                textBoxInputManager.UseParentInput = false;
                return;
            }

            textBoxInputManager.ChangeFocus(textBox);
            obtainedFocus = true;

            updateVisibility();
        }

        public void ReleaseFocus()
        {
            textBoxInputManager.ChangeFocus(null);
            SearchTerm.Value = string.Empty;
            obtainedFocus = false;

            updateVisibility();
        }

        public bool Back()
        {
            // text box may have lost focus from pressing escape, retain it.
            if (obtainedFocus && !textBox.HasFocus)
                ObtainFocus();

            if (!string.IsNullOrEmpty(SearchTerm.Value))
            {
                SearchTerm.Value = string.Empty;
                return true;
            }

            return false;
        }

        private void updateVisibility() => State.Value = obtainedFocus && (AlwaysDisplayOnFocus || !string.IsNullOrEmpty(SearchTerm.Value))
            ? Visibility.Visible
            : Visibility.Hidden;

        protected abstract TextBox CreateTextBox();
    }
}
