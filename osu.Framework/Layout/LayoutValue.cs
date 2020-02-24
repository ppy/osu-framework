// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Layout
{
    public class LayoutValue : LayoutMember
    {
        public LayoutValue(Invalidation invalidationType, InvalidationConditionDelegate invalidationCondition = null)
            : base(invalidationType, invalidationCondition)
        {
        }

        public new void Validate() => base.Validate();
    }

    public class LayoutValue<T> : LayoutMember
    {
        public LayoutValue(Invalidation invalidationType, InvalidationConditionDelegate invalidationCondition = null)
            : base(invalidationType, invalidationCondition)
        {
        }

        private T value;

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
