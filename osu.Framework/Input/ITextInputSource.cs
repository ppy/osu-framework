// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Input
{
    /// <summary>
    /// A source from which we can retrieve user text input.
    /// Generally hides a native implementation from the game framework.
    /// </summary>
    public interface ITextInputSource
    {
        bool ImeActive { get; }

        string GetPendingText();

        void Activate();

        /// <summary>
        /// Ensures that the native implementation that retrieves user text input is activated
        /// and that the user can start entering text.
        /// </summary>
        void EnsureActivated();

        void Deactivate();

        event Action<string> OnNewImeComposition;
        event Action<string> OnNewImeResult;
    }
}
