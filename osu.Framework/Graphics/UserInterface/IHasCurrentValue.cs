// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A UI element which supports a <see cref="Bindable{T}"/> current value.
    /// You can bind to <see cref="Current"/>'s <see cref="Bindable{T}.ValueChanged"/> to get updates.
    /// </summary>
    public interface IHasCurrentValue<T>
    {
        /// <summary>
        /// Gets or sets the <see cref="Bindable{T}"/> that provides the current value.
        /// </summary>
        /// <remarks>
        /// A provided <see cref="Bindable{T}"/> will be bound to, rather than be stored internally.
        /// </remarks>
        Bindable<T> Current { get; set; }
    }
}
