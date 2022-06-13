// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Layout
{
    /// <summary>
    /// A member that represents the validation state of the layout of a <see cref="Drawable"/>.
    /// Can be invalidated according to state changes in a <see cref="Drawable"/> (via <see cref="Invalidation"/> flags), and validated on-demand when the layout has been re-computed.
    /// </summary>
    public class LayoutValue : LayoutMember
    {
        /// <summary>
        /// Creates a new <see cref="LayoutValue"/>.
        /// </summary>
        /// <param name="invalidation">The <see cref="Invalidation"/> flags that will invalidate this <see cref="LayoutValue"/>.</param>
        /// <param name="source">The source of the invalidation.</param>
        /// <param name="conditions">Any extra conditions that must be satisfied before this <see cref="LayoutValue"/> is invalidated.</param>
        public LayoutValue(Invalidation invalidation, InvalidationSource source = InvalidationSource.Default, InvalidationConditionDelegate conditions = null)
            : base(invalidation, source, conditions)
        {
        }

        /// <summary>
        /// Validates this <see cref="LayoutValue"/>.
        /// </summary>
        public new void Validate() => base.Validate();
    }

    /// <summary>
    /// A member that represents the validation state of a value in the layout of a <see cref="Drawable"/>.
    /// Can be invalidated according to state changes in a <see cref="Drawable"/> (via <see cref="Invalidation"/> flags), and validated when an up-to-date value is set.
    /// </summary>
    /// <typeparam name="T">The type of value stored.</typeparam>
    public class LayoutValue<T> : LayoutMember
    {
        /// <summary>
        /// Creates a new <see cref="LayoutValue{T}"/>.
        /// </summary>
        /// <param name="invalidation">The <see cref="Invalidation"/> flags that will invalidate this <see cref="LayoutValue{T}"/>.</param>
        /// <param name="source">The source of the invalidation.</param>
        /// <param name="conditions">Any extra conditions that must be satisfied before this <see cref="LayoutValue{T}"/> is invalidated.</param>
        public LayoutValue(Invalidation invalidation, InvalidationSource source = InvalidationSource.Default, InvalidationConditionDelegate conditions = null)
            : base(invalidation, source, conditions)
        {
        }

        private T value;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        /// <exception cref="InvalidOperationException">If accessed while <see cref="LayoutMember.IsValid"/> is <code>false</code>.</exception>
        public T Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException($"May not query {nameof(Value)} of an invalid {nameof(LayoutValue<T>)}.");

                return value;
            }
            set
            {
                this.value = value;

                Validate();
            }
        }

        public static implicit operator T(LayoutValue<T> layoutValue) => layoutValue.Value;
    }
}
