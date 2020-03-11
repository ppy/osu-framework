// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An interface which can be assigned to <see cref="IBindable"/>s to add property guards on <see cref="BindableProperty{T}.Value"/> setter.
    /// </summary>
    /// <remarks>
    /// This interface only has protected methods to be executed internally inside <see cref="BindableProperty{T}"/>.
    /// </remarks>
    public interface IHasPropertyGuards
    {
        /// <summary>
        /// The conditions required to allow changing <see cref="property"/>'s value to <see cref="value"/>.
        /// Must throw exception inside the method for rejecting value change.
        /// </summary>
        protected internal void CheckPropertyValueChange<TValue>(IBindableProperty<TValue> property, TValue value)
        {
        }
    }
}
