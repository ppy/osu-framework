// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that may be attached to a class definitions or fields of a <see cref="Drawable"/> to indicate that the value should be cached as a dependency.
    /// Cached values may be resolved through <see cref="BackgroundDependencyLoaderAttribute"/> or <see cref="ResolvedAttribute"/>.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
    public class CachedAttribute : Attribute
    {
        internal const BindingFlags ACTIVATOR_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// The type to cache the value as. If null, the value will be cached as the value's most derived type.
        /// </summary>
        /// <example>
        /// For example, if this value is null on the following field definition:
        ///
        /// private BaseType obj = new DerivedType()
        ///
        /// Then the cached type will be "DerivedType".
        /// </example>
        public Type Type;

        public string Name;

        internal static CacheDependencyDelegate CreateActivator(Type type)
        {
            var additionActivators = new List<Action<object, DependencyContainer, CacheInfo>>();

            // Types within the framework should be able to cache value types if they desire (e.g. cancellation tokens)
            var allowValueTypes = type.Assembly == typeof(Drawable).Assembly;

            foreach (var attribute in type.GetCustomAttributes<CachedAttribute>())
                additionActivators.Add((target, dc, info) => dc.CacheAs(attribute.Type ?? type, new CacheInfo(attribute.Name, info.Parent), target, allowValueTypes));

            foreach (var field in type.GetFields(ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
            {
                var modifier = field.GetAccessModifier();
                if (modifier != AccessModifier.Private && !field.IsInitOnly)
                    throw new AccessModifierNotAllowedForCachedValueException(modifier, field);

                foreach (var attribute in field.GetCustomAttributes<CachedAttribute>())
                {
                    additionActivators.Add((target, dc, info) =>
                    {
                        var value = field.GetValue(target);

                        if (value == null)
                        {
                            if (allowValueTypes)
                                return;
                            throw new NullReferenceException($"Attempted to cache a null value: {type.ReadableName()}.{field.Name}.");
                        }

                        var cacheInfo = new CacheInfo(attribute.Name);
                        if (info.Parent != null)
                        {
                            // When a parent type exists, infer the property name if one is not provided
                            cacheInfo = new CacheInfo(cacheInfo.Name ?? field.Name, info.Parent);
                        }

                        dc.CacheAs(attribute.Type ?? value.GetType(), cacheInfo, value, allowValueTypes);
                    });
                }
            }

            if (additionActivators.Count == 0)
                return (_, existing, info) => existing;

            return (target, existing, info) =>
            {
                var dependencies = new DependencyContainer(existing);
                foreach (var a in additionActivators)
                    a(target, dependencies, info);

                return dependencies;
            };
        }
    }
}
