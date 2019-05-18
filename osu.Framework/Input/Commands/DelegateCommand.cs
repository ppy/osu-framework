// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;

namespace osu.Framework.Input.Commands
{
    /// <summary>
    /// An <see cref="ICommand"/> whose delegates do not take any parameters for <see cref="Execute"/>.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        private readonly Action _executeMethod;

        public DelegateCommand(Action executeMethod)
        {
            _executeMethod = executeMethod;
            CanExecute = new Bindable<bool>();
        }

        public void Execute()
        {
            _executeMethod?.Invoke();
        }

        public Bindable<bool> CanExecute { get; }
    }
}
