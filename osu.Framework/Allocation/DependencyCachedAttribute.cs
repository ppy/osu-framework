// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that may be attached to a class definitions, fields, or properties of a <see cref="Drawable"/> to indicate
    /// that the value should be cached as a dependency.
    /// Cached values may be retrieved through <see cref="BackgroundDependencyLoaderAttribute"/> or <see cref="DependencyAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class DependencyCachedAttribute : Attribute
    {
        /// <summary>
        /// The type to cache the value as.
        /// </summary>
        public readonly Type CachedType;

        /// <summary>
        /// Constructs a new <see cref="DependencyCachedAttribute"/>.
        /// The type of the cached value matches that of the member which this attribute is attached on.
        /// </summary>
        public DependencyCachedAttribute()
        {
        }

        /// <summary>
        /// Constructs a new <see cref="DependencyCachedAttribute"/>.
        /// The type of the cached value will match a given type.
        /// </summary>
        /// <param name="cachedType">The type to cache the value as.</param>
        public DependencyCachedAttribute(Type cachedType)
        {
            CachedType = cachedType;
        }
    }
}
