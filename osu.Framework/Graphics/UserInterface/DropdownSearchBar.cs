// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract partial class DropdownSearchBar : VisibilityContainer
    {
        private TextBox textBox = null!;
        private PassThroughInputManager textBoxInputManager = null!;

        public Bindable<string> SearchTerm { get; } = new Bindable<string>();

        // handling mouse input on dropdown header is not easy, since the menu would lose focus on release and automatically close
        public override bool HandlePositionalInput => false;
        public override bool PropagatePositionalInputSubTree => false;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            // Dropdown menus rely on their focus state to determine when they should be closed.
            // On the other hand, text boxes require to be focused in order for the user to interact with them.
            // To handle that matter, we'll wrap the search text box inside a local input manager, and manage its focus state accordingly.
            InternalChild = textBoxInputManager = new PassThroughInputManager
            {
                RelativeSizeAxes = Axes.Both,
                Child = textBox = CreateTextBox().With(t =>
                {
                    t.RelativeSizeAxes = Axes.Both;
                    t.Current = SearchTerm;
                })
            };
        }

        public void Focus() => textBoxInputManager.ChangeFocus(textBox);

        public void Reset()
        {
            textBoxInputManager.ChangeFocus(null);
            textBox.Text = string.Empty;

            Hide();
        }

        protected abstract TextBox CreateTextBox();
    }
}
