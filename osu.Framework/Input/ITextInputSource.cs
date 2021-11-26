// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Input
{
    /// <summary>
    /// A source from which we can retrieve user text input.
    /// Generally hides a native implementation from the game framework.
    /// </summary>
    public interface ITextInputSource
    {
        /// <summary>
        /// Whether the IME is actively providing text composition through <see cref="OnNewImeComposition"/> and accepting input from the user.
        /// </summary>
        bool ImeActive { get; }

        string GetPendingText();

        void Activate();

        /// <summary>
        /// Ensures that the native implementation that retrieves user text input is activated
        /// and that the user can start entering text.
        /// </summary>
        void EnsureActivated();

        void Deactivate();

        /// <summary>
        /// Sets where the native implementation displays the IME and other text input elements.
        /// </summary>
        /// <param name="rectangle">Should be provided in screen space.</param>
        void SetImeRectangle(RectangleF rectangle);

        /// <summary>
        /// Resets the IME.
        /// This clears the current composition string and prepares it for new input.
        /// </summary>
        void ResetIme();

        /// <summary>
        /// Invoked when the IME composition starts or changes.
        /// </summary>
        /// <remarks>Empty string for text means that the composition has been cancelled.</remarks>
        event ImeCompositionDelegate OnNewImeComposition;

        /// <summary>
        /// Invoked when the IME composition successfully completes.
        /// </summary>
        event Action<string> OnNewImeResult;

        /// <summary>
        /// Fired on a new IME composition.
        /// </summary>
        /// <param name="text">The composition text.</param>
        /// <param name="start">The index of the selection start.</param>
        /// <param name="length">The length of the selection.</param>
        /// <remarks>Empty string for <paramref name="text"/> means that the composition has been cancelled.</remarks>
        public delegate void ImeCompositionDelegate(string text, int start, int length);
    }
}
