// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An attribute that may be attached to a class definitions, fields, or properties of a <see cref="Drawable"/> to indicate that the value should be cached as a dependency.
    /// Cached values may be resolved through <see cref="BackgroundDependencyLoaderAttribute"/> or <see cref="ResolvedAttribute"/>.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
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

        /// <summary>
        /// The name to identify this member with.
        /// </summary>
        /// <remarks>
        /// If the member is cached with a custom <see cref="CacheInfo"/> that provides a parent, the name is automatically inferred from the field/property.
        /// </remarks>
        public string Name;

        /// <summary>
        /// Identifies a member to be cached to a <see cref="DependencyContainer"/>.
        /// </summary>
        public CachedAttribute()
        {
        }

        /// <summary>
        /// Identifies a member to be cached to a <see cref="DependencyContainer"/>.
        /// </summary>
        /// <param name="type">The type to cache the member as.</param>
        /// <param name="name">The name to identify the member as in the cache.</param>
        public CachedAttribute(Type type = null, string name = null)
        {
            Type = type;
            Name = name;
        }

        internal static CacheDependencyDelegate CreateActivator(Type type)
        {
            var additionActivators = new List<Action<object, DependencyContainer, CacheInfo>>();

            // Types within the framework should be able to cache value types if they desire (e.g. cancellation tokens)
            bool allowValueTypes = type.Assembly == typeof(Drawable).Assembly;

            foreach (var iface in type.GetInterfaces())
            {
                foreach (var attribute in iface.GetCustomAttributes<CachedAttribute>())
                    additionActivators.Add((target, dc, info) => dc.CacheAs(attribute.Type ?? iface, new CacheInfo(info.Name ?? attribute.Name, info.Parent), target, allowValueTypes));
            }

            foreach (var attribute in type.GetCustomAttributes<CachedAttribute>())
                additionActivators.Add((target, dc, info) => dc.CacheAs(attribute.Type ?? type, new CacheInfo(info.Name ?? attribute.Name, info.Parent), target, allowValueTypes));

            foreach (var property in type.GetProperties(ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                additionActivators.AddRange(createMemberActivator(property, type, allowValueTypes));

            foreach (var field in type.GetFields(ACTIVATOR_FLAGS).Where(f => f.GetCustomAttributes<CachedAttribute>().Any()))
                additionActivators.AddRange(createMemberActivator(field, type, allowValueTypes));

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

        private static IEnumerable<Action<object, DependencyContainer, CacheInfo>> createMemberActivator(MemberInfo member, Type type, bool allowValueTypes)
        {
            switch (member)
            {
                case PropertyInfo pi:
                {
                    var getMethod = pi.GetMethod;
                    if (getMethod == null)
                        throw new AccessModifierNotAllowedForCachedValueException(AccessModifier.None, pi);

                    if (getMethod.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                        throw new AccessModifierNotAllowedForCachedValueException(AccessModifier.None, pi);

                    var setMethod = pi.SetMethod;

                    if (setMethod != null)
                    {
                        var modifier = setMethod.GetAccessModifier();
                        if (modifier != AccessModifier.Private)
                            throw new AccessModifierNotAllowedForCachedValueException(modifier, setMethod);

                        if (setMethod.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                            throw new AccessModifierNotAllowedForCachedValueException(AccessModifier.None, pi);
                    }

                    break;
                }

                case FieldInfo fi:
                {
                    var modifier = fi.GetAccessModifier();
                    if (modifier != AccessModifier.Private && !fi.IsInitOnly)
                        throw new AccessModifierNotAllowedForCachedValueException(modifier, fi);

                    break;
                }
            }

            foreach (var attribute in member.GetCustomAttributes<CachedAttribute>())
            {
                yield return (target, dc, info) =>
                {
                    object value = null;

                    if (member is PropertyInfo p)
                        value = p.GetValue(target);

                    if (member is FieldInfo f)
                        value = f.GetValue(target);

                    if (value == null)
                    {
                        if (allowValueTypes)
                            return;

                        throw new NullDependencyException($"Attempted to cache a null value: {type.ReadableName()}.{member.Name}.");
                    }

                    var cacheInfo = new CacheInfo(info.Name ?? attribute.Name);

                    if (info.Parent != null)
                    {
                        // When a parent type exists, infer the property name if one is not provided
                        cacheInfo = new CacheInfo(cacheInfo.Name ?? member.Name, info.Parent);
                    }

                    dc.CacheAs(attribute.Type ?? value.GetType(), cacheInfo, value, allowValueTypes);
                };
            }
        }
    }

    /// <summary>
    /// The exception that is thrown when attempting to cache <see langword="null"/> using <see cref="CachedAttribute"/>.
    /// </summary>
    public sealed class NullDependencyException : InvalidOperationException
    {
        public NullDependencyException(string message)
            : base(message)
        {
        }
    }
}
