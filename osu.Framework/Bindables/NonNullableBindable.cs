// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Bindables
{
    public class NonNullableBindable<T> : Bindable<T>
        where T : class
    {
        public NonNullableBindable(T defaultValue)
            : base(defaultValue)
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));
        }

        protected override void CheckPropertyValueChange<TValue>(IBindableProperty<TValue> property, TValue value)
        {
            base.CheckPropertyValueChange(property, value);

            if (value == null)
                throw new ArgumentNullException(nameof(value), $"Cannot set {null} value to {nameof(NonNullableBindable<T>)} properties.");
        }
    }
}
