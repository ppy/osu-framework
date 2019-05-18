// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Input.Commands
{
    /// <summary>
    /// Defines a command.
    /// </summary>
    public interface ICommand
    {
        void Execute();
        Bindable<bool> CanExecute { get; }
    }
}
