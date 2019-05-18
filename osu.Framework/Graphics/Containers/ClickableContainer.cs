// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.Commands;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Containers
{
    public class ClickableContainer : Container
    {
        private ICommand command;

        public ICommand Command
        {
            get => command;
            set
            {
                if (command != null)
                    Enabled.UnbindFrom(command.CanExecute);

                Enabled.BindTo((command = value).CanExecute);
            }
        }

        public readonly BindableBool Enabled = new BindableBool();

        protected override bool OnClick(ClickEvent e)
        {
            if (Enabled.Value)
                Command?.Execute();
            return true;
        }
    }
}
