// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Bindables
{
    public class NonNullableBindable<T> : Bindable<T>
        where T : class
    {
        public NonNullableBindable(T defaultValue)
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            Value = Default = defaultValue;
        }

        private NonNullableBindable()
        {
        }

        public override T Value
        {
            get => base.Value;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value), $"Cannot set {nameof(Value)} of a {nameof(NonNullableBindable<T>)} to null.");

                base.Value = value;
            }
        }

        protected override Bindable<T> CreateInstance() => new NonNullableBindable<T>();
    }
}
