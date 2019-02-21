// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    public class ValueChangedEvent<T>
    {
        /// <summary>
        /// The old value.
        /// </summary>
        public readonly T From;

        /// <summary>
        /// The new (and current) value.
        /// </summary>
        public readonly T To;

        public ValueChangedEvent(T from, T to)
        {
            From = from;
            To = to;
        }
    }
}
