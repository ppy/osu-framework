// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public class NonNullableBindable<T> : Bindable<T>
    {
        public NonNullableBindable(T defaultValue)
        {
            if (defaultValue == null)
                throw new ArgumentNullException(nameof(defaultValue));

            Value = Default = defaultValue;
        }
        public override T Value
        {
            get
            {
                return base.Value;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(Value), $"Cannot set {nameof(Value)} of a {nameof(NonNullableBindable<T>)} to null.");

                base.Value = value;
            }
        }
    }
}
