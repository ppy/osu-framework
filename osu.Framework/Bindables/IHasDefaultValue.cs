// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Interface for objects that have a default value.
    /// </summary>
    public interface IHasDefaultValue
    {
        /// <summary>
        /// Check whether this object has its default value.
        /// </summary>
        bool IsDefault { get; }
    }
}
