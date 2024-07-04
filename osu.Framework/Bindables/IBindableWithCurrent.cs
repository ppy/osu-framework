// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;

namespace osu.Framework.Bindables
{
    public interface IBindableWithCurrent<T> : IBindable<T>, IHasCurrentValue<T>
    {
        /// <summary>
        /// Creates a new <see cref="IBindableWithCurrent{T}"/> according to the specified value type.
        /// If the value type is one supported by the <see cref="BindableNumber{T}"/>, an instance of <see cref="BindableNumberWithCurrent{T}"/> will be returned.
        /// Otherwise an instance of <see cref="BindableWithCurrent{T}"/> will be returned instead.
        /// </summary>
        public static IBindableWithCurrent<T> Create()
        {
            if (Validation.IsSupportedBindableNumberType<T>())
                return (IBindableWithCurrent<T>)Activator.CreateInstance(typeof(BindableNumberWithCurrent<>).MakeGenericType(typeof(T)), default(T));

            return new BindableWithCurrent<T>();
        }
    }
}
