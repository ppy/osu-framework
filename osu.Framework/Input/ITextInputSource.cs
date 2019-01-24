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

        event Action<string> OnNewImeComposition;
        event Action<string> OnNewImeResult;
    }
}
