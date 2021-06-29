// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;

namespace osu.Framework.Bindables
{
    public interface IBindableWithCurrent<T> : IBindable<T>
    {
        /// <summary>
        /// Gets or sets the <see cref="Bindable{T}"/> that provides the current value.
        /// </summary>
        /// <remarks>
        /// A provided <see cref="Bindable{T}"/> will be bound to, rather than be stored internally.
        /// </remarks>
        Bindable<T> Current { get; set; }

        /// <summary>
        /// Creates a new <see cref="IBindableWithCurrent{T}"/> according to the specified value type.
        /// If the value type is one supported by the <see cref="BindableNumber{T}"/>, an instance of <see cref="BindableNumberWithCurrent{T}"/> will be returned.
        /// Otherwise an instance of <see cref="BindableWithCurrent{T}"/> will be returned instead.
        /// </summary>
        public static IBindableWithCurrent<T> Create()
        {
            if (Validation.IsSupportedBindableNumberType<T>())
                return (IBindableWithCurrent<T>)Activator.CreateInstance(typeof(BindableNumberWithCurrent<>).MakeGenericType(typeof(T)));

            return new BindableWithCurrent<T>();
        }
    }
}
