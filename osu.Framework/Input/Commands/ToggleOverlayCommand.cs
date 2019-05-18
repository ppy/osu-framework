// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Input.Commands
{
    /// <summary>
    /// An <see cref="ICommand"/> for toggling <see cref="OverlayContainer"/> visibility.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ToggleOverlayCommand<T> : DelegateCommand where T : OverlayContainer
    {
        public ToggleOverlayCommand(T overlay)
            : base(overlay.ToggleVisibility)
        {
        }
    }
}
