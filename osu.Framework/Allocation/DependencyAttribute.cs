// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that is attached to fields of a <see cref="Drawable"/> component to indicate
    /// that the value of the field should be retrieved from a dependency cache.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Field)]
    public class DependencyAttribute : Attribute
    {
        /// <summary>
        /// Whether a null value can be accepted if the value does not exist in the cache.
        /// </summary>
        public bool CanBeNull { get; set; }
    }
}
