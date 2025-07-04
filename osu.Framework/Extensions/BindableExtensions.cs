// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;

namespace osu.Framework.Extensions
{
    public static class BindableExtensions
    {
        /// <summary>
        /// Creates a readonly <see cref="IBindable{T}"/> with it's value automatically assigned from the source <see cref="IBindable{T}"/> and converted using the given transform function.
        /// </summary>
        public static IBindable<TDest> Map<TSource, TDest>(this IBindable<TSource> source, Func<TSource, TDest> transform)
        {
            var dest = new Bindable<TDest>();

            dest.ComputeFrom(source, transform);

            return dest;
        }

        /// <summary>
        /// Binds a <see cref="Bindable{T}"/> to another <see cref="IBindable{T}"/> with the value automatically converted using the given transform function.
        /// </summary>
        public static void ComputeFrom<TSource, TDest>(this Bindable<TDest> dest, IBindable<TSource> source, Func<TSource, TDest> transform)
        {
            source.BindValueChanged(e =>
            {
                dest.Value = transform(e.NewValue);
            }, true);

            source.BindDisabledChanged(disabled =>
            {
                dest.Disabled = disabled;
            }, true);
        }
    }
}
