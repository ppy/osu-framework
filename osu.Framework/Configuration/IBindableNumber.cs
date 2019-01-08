// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public interface IBindableNumber<T> : IBindable<T>
    {
        /// <summary>
        /// An event which is raised when <see cref="Precision"/> has changed.
        /// </summary>
        event Action<T> PrecisionChanged;

        /// <summary>
        /// An event which is raised when <see cref="MinValue"/> has changed.
        /// </summary>
        event Action<T> MinValueChanged;

        /// <summary>
        /// An event which is raised when <see cref="MaxValue"/> has changed.
        /// </summary>
        event Action<T> MaxValueChanged;

        /// <summary>
        /// The precision up to which the value of this bindable should be rounded.
        /// </summary>
        T Precision { get; }

        /// <summary>
        /// The minimum value of this bindable. <see cref="IBindableNumber{T}.Value"/> will never go below this value.
        /// </summary>
        T MinValue { get; }

        /// <summary>
        /// The maximum value of this bindable. <see cref="IBindableNumber{T}"/> will never go above this value.
        /// </summary>
        T MaxValue { get; }

        /// <summary>
        /// Whether <typeparamref name="T"/> is an integer.
        /// </summary>
        bool IsInteger { get; }

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        new IBindableNumber<T> GetBoundCopy();
    }
}
