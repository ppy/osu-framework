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
                var fieldGetter = getDependency(property.PropertyType, type, attribute.CanBeNull || property.PropertyType.IsNullable());

                activators.Add((target, dc) => property.SetValue(target, fieldGetter(dc)));
            }

            return (target, dc) => activators.ForEach(a => a(target, dc));
        }

        private static Func<IReadOnlyDependencyContainer, object> getDependency(Type type, Type requestingType, bool permitNulls) => dc =>
        {
            var val = dc.Get(type);
            if (val == null && !permitNulls)
                throw new DependencyNotRegisteredException(requestingType, type);
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
