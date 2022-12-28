// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Stores rendering state such as <see cref="IRawVertexBuffer"/> layouts or the bound <see cref="IRawIndexBuffer"/>.
    /// This allows to reduce the amount of draw calls.
    /// </summary>
    /// <remarks>
    /// Only one state array may be bound at one time.
    /// This is subject to renderer specific optimisations and performing any actions related to the cached state
    /// while an <see cref="IRenderStateArray"/> is bound might result in undefined behaviour unless specified otherwise.
    /// </remarks>
    public interface IRenderStateArray : IDisposable
    {
        /// <summary>
        /// The state which will be cached by this state array.
        /// </summary>
        StateArrayFlags CachedState { get; }

        /// <summary>
        /// Binds the state array, restoring the state cached by it.
        /// This also restores the state from before the previously bound state array was bound.
        /// </summary>
        /// <remarks>
        /// While a state array is bound all actions related to <see cref="CachedState"/> will be cached.
        /// </remarks>
        /// <returns>Whether the bind was necessary.</returns>
        bool Bind();

        /// <summary>
        /// Unbinds this state array. This restores the state from before any state array was bound.
        /// </summary>
        void Unbind();
    }
}
