// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that may be attached to a class definitions, fields, or properties of a <see cref="Drawable"/> to indicate
    /// that the value should be cached as a dependency.
    /// Cached values may be retrieved through <see cref="BackgroundDependencyLoaderAttribute"/> or <see cref="DependencyAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class DependencyCachedAttribute : Attribute
    {
        private const BindingFlags activator_flags = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// The type to cache the value as.
        /// </summary>
        private readonly Type cachedType;

        /// <summary>
        /// Constructs a new <see cref="DependencyCachedAttribute"/>.
        /// </summary>
        /// <param name="cachedType">The type to cache the value as.
        /// If this is null, the cached type will match the type of the member which the attribute is attached to.</param>
        public DependencyCachedAttribute(Type cachedType = null)
        {
            this.cachedType = cachedType;
        }

        internal static CacheDependencyDelegate CreateActivator(Type type)
        {
            var additionActivators = new List<Action<object, DependencyContainer>>();

            foreach (var attribute in type.GetCustomAttributes<DependencyCachedAttribute>())
                additionActivators.Add((target, dc) => dc.CacheAs(attribute.cachedType ?? type, target));

            foreach (var field in type.GetFields(activator_flags).Where(f => f.GetCustomAttributes<DependencyCachedAttribute>().Any()))
            foreach (var attribute in field.GetCustomAttributes<DependencyCachedAttribute>())
                additionActivators.Add((target, dc) => dc.CacheAs(attribute.cachedType ?? field.FieldType, field.GetValue(target)));

            if (additionActivators.Count == 0)
                return (_, existing) => existing;

            return (target, existing) =>
            {
                var dependencies = new DependencyContainer(existing);
                additionActivators.ForEach(a => a(target, dependencies));

                return dependencies;
            };
        }
    }
}
