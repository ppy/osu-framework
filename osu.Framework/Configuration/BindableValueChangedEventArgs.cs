// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public class BindableValueChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// The old value of the <see cref="Bindable{T}"/>.
        /// </summary>
        public readonly T From;

        /// <summary>
        /// The new (and current) value of the <see cref="Bindable{T}"/>.
        /// </summary>
        public readonly T To;

        public BindableValueChangedEventArgs(T from, T to)
        {
            From = from;
            To = to;
        }
    }
}
