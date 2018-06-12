// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input
{
    /// <summary>
    /// Denotes an input from <see cref="Handlers.InputHandler"/>.
    /// </summary>
    public interface IInput
    {
        /// <summary>
        /// Apply input to an <see cref="InputState"/>.
        /// This changes <paramref name="state"/> and the change made will be reported to <paramref name="handler"/>.
        /// </summary>
        void Apply(InputState state, IInputStateChangeHandler handler);
    }
}
