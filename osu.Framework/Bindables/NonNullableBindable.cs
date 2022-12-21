// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Bindables
{
    public class NonNullableBindable<T> : Bindable<T>
        where T : class
    {
        public NonNullableBindable(T defaultValue)
        {
            ArgumentNullException.ThrowIfNull(defaultValue);

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
                ArgumentNullException.ThrowIfNull(value);

                base.Value = value;
            }
        }

        protected override Bindable<T> CreateInstance() => new NonNullableBindable<T>();
    }
}
