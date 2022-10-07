// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Interface for objects that have a disabled state.
    /// </summary>
    public interface ICanBeDisabled
    {
        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed.
        /// </summary>
        event Action<bool> DisabledChanged;

        /// <summary>
        /// Bind an action to <see cref="DisabledChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Disabled"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false);

        /// <summary>
        /// Whether this object has been disabled.
        /// </summary>
        bool Disabled { get; }
    }
}
