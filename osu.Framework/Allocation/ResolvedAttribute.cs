// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that is attached to properties of a <see cref="Drawable"/> component to indicate that the value of the property should be retrieved from a dependency cache.
    /// Properties marked with this attribute must be private and have a setter.
    /// </summary>
    /// <remarks>
    /// The value of the property is resolved upon <see cref="Drawable.Load"/> for the target <see cref="Drawable"/>.
    /// </remarks>
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    [AttributeUsage(AttributeTargets.Property)]
    public class ResolvedAttribute : Attribute
    {
        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// The containing type of the cached member in the <see cref="DependencyContainer"/>.
        /// </summary>
        /// <remarks>
        /// This is only set if the member was cached with a custom <see cref="CacheInfo"/>.
        /// </remarks>
        public Type Parent;

        /// <summary>
        /// The name of the cached member in the <see cref="DependencyContainer"/>.
        /// </summary>
        /// <remarks>
        /// This is only set if the member was cached with a custom <see cref="CacheInfo"/>.
        /// </remarks>
        public string Name;

        /// <summary>
        /// Whether a null value can be accepted if the member doesn't exist in the cache.
        /// </summary>
        public bool CanBeNull;

        /// <summary>
        /// Identifies a member to be resolved from a <see cref="DependencyContainer"/>.
        /// </summary>
        public ResolvedAttribute()
        {
        }

        /// <summary>
        /// Identifies a member to be resolved from a <see cref="DependencyContainer"/>.
        /// </summary>
        /// <param name="parent">The parent which the member is identified with in the cache.
        /// This is only set if the member was cached with a custom <see cref="CacheInfo"/>.</param>
        /// <param name="name">The name which the member is identified with in the cache.
        /// This is only set if the member was cached with a custom <see cref="CacheInfo"/>.</param>
        /// <param name="canBeNull">Whether a null value can be accepted if the member doesn't exist in the cache.</param>
        public ResolvedAttribute(Type parent = null, string name = null, bool canBeNull = false)
        {
            Parent = parent;
            Name = name;
            CanBeNull = canBeNull;
        }

        internal static InjectDependencyDelegate CreateActivator(Type type)
        {
            var activators = new List<Action<object, IReadOnlyDependencyContainer>>();

            var properties = type.GetProperties(activator_flags).Where(f => f.GetCustomAttribute<ResolvedAttribute>() != null);

            foreach (var property in properties)
            {
                if (!property.CanWrite)
                    throw new PropertyNotWritableException(type, property.Name);

                var modifier = property.SetMethod.GetAccessModifier();
                if (modifier != AccessModifier.Private)
                    throw new AccessModifierNotAllowedForPropertySetterException(modifier, property);

                var attribute = property.GetCustomAttribute<ResolvedAttribute>();
                Debug.Assert(attribute != null);

                var cacheInfo = new CacheInfo(attribute.Name);

                if (attribute.Parent != null)
                {
                    // When a parent type exists, infer the property name if one is not provided
                    cacheInfo = new CacheInfo(cacheInfo.Name ?? property.Name, attribute.Parent);
                }

                var fieldGetter = getDependency(property.PropertyType, type, attribute.CanBeNull || property.IsNullable(), cacheInfo);

                activators.Add((target, dc) => property.SetValue(target, fieldGetter(dc)));
            }

            return (target, dc) =>
            {
                foreach (var a in activators)
                    a(target, dc);
            };
        }

        private static Func<IReadOnlyDependencyContainer, object> getDependency(Type type, Type requestingType, bool permitNulls, CacheInfo info) => dc =>
        {
            object val = dc.Get(type, info);
            if (val == null && !permitNulls)
                throw new DependencyNotRegisteredException(requestingType, type);

            if (val is IBindable bindableVal)
                return bindableVal.GetBoundCopy();

            return val;
        };
    }

    public class PropertyNotWritableException : Exception
    {
        public PropertyNotWritableException(Type type, string propertyName)
            : base($"Attempting to inject dependencies into non-write-able property {propertyName} of type {type.ReadableName()}.")
        {
        }
    }
}
