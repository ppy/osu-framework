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

        void Deactivate(object sender);

        void Activate(object sender);

        /// <summary>
        /// Requests the OS to show the on-screen/software keyboard.
        /// </summary>
        /// <remarks>
        /// The OS is hopefully smart enough not to show the software keyboard if a hardware one is present.
        /// Should be reguraly called when doing text editing operations, as the user might have manually closed the software keyboard.
        /// </remarks>
        void ShowSoftKeyboard();

        event Action<string> OnNewImeComposition;
        event Action<string> OnNewImeResult;
    }
}
