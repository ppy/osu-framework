// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An interface representing a read-only <see cref="BindableProperty{T}"/>.
    /// </summary>
    public interface IBindableProperty
    {
        /// <summary>
        /// The bindable source of this property.
        /// </summary>
        IBindable Source { get; }

        /// <summary>
        /// The current value of this property.
        /// </summary>
        object Value { get; }
    }

    /// <inheritdoc />
    /// <typeparam name="T">The property value type.</typeparam>
    public interface IBindableProperty<out T> : IBindableProperty
    {
        /// <summary>
        /// The current value of this property.
        /// </summary>
        new T Value { get; }
    }
}
