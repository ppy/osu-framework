// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using osu.Framework.Configuration;
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
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Property)]
    public class ResolvedAttribute : Attribute
    {
        private const BindingFlags activator_flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Whether a null value can be accepted if the value does not exist in the cache.
        /// </summary>
        public bool CanBeNull;

        public string Name;

        public Type Parent;

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

                var cacheInfo = new CacheInfo(attribute.Name);
                if (attribute.Parent != null)
                {
                    // When a parent type exists, infer the property name if one is not provided
                    cacheInfo = new CacheInfo(cacheInfo.Name ?? property.Name, attribute.Parent);
                }

                var fieldGetter = getDependency(property.PropertyType, type, attribute.CanBeNull || property.PropertyType.IsNullable(), cacheInfo);

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
            var val = dc.Get(type, info);
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
