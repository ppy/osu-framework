// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An interface that represents a read-only leased bindable.
    /// </summary>
    public interface ILeasedBindable : IBindable
    {
        /// <summary>
        /// End the lease on the source <see cref="Bindable{T}"/>.
        /// </summary>
        void Return();
    }
}
