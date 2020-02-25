// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Layout
{
    public class LayoutValue : LayoutMember
    {
        /// <summary>
        /// Creates a new <see cref="LayoutValue"/>.
        /// </summary>
        /// <param name="invalidationType">The <see cref="Invalidation"/> flags that will invalidate this <see cref="LayoutValue"/>.</param>
        /// <param name="invalidationCondition">Any extra conditions that must be satisfied before this <see cref="LayoutValue"/> is invalidated.</param>
        /// <param name="invalidationSource">The source of the invalidation.</param>
        public LayoutValue(Invalidation invalidationType, InvalidationConditionDelegate invalidationCondition = null, InvalidationSource invalidationSource = InvalidationSource.Default)
            : base(invalidationType, invalidationCondition)
        {
        }

        /// <summary>
        /// Validates this <see cref="LayoutValue"/>.
        /// </summary>
        public new void Validate() => base.Validate();
    }

    public class LayoutValue<T> : LayoutMember
    {
        /// <summary>
        /// Creates a new <see cref="LayoutValue{T}"/>.
        /// </summary>
        /// <param name="invalidationType">The <see cref="Invalidation"/> flags that will invalidate this <see cref="LayoutValue{T}"/>.</param>
        /// <param name="invalidationCondition">Any extra conditions that must be satisfied before this <see cref="LayoutValue{T}"/> is invalidated.</param>
        /// <param name="invalidationSource">The source of the invalidation.</param>
        public LayoutValue(Invalidation invalidationType, InvalidationConditionDelegate invalidationCondition = null, InvalidationSource invalidationSource = InvalidationSource.Default)
            : base(invalidationType, invalidationCondition)
        {
        }

        private T value;

        /// <summary>
        /// The current value.
        /// </summary>
        /// <exception cref="InvalidOperationException">If accessed while <see cref="LayoutMember.IsValid"/> is <code>false</code>.</exception>
        public T Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException("todo");

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
