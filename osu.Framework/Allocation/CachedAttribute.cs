// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    /// An attribute that may be attached to a class, interface, field, or property definitions of a <see cref="Drawable"/>
    /// to indicate that the value should be cached as a dependency.
    /// Cached values may be resolved through <see cref="BackgroundDependencyLoaderAttribute"/> or <see cref="ResolvedAttribute"/>.
    /// </summary>
    /// <remarks>
    /// The behaviour of this attribute differs in meaning depending on the type of member it is placed on.
    /// <list type="bullet">
    /// <item>
    /// <para>
    /// In the case of fields and properties, the dependency will be cached for the children of the drawable that contains the field or property.
    /// </para>
    /// <para>
    /// Unless specified differently by <see cref="Type"/>, the dependency will be cached using the field/property value's concrete (most-derived) type.
    /// See the examples section of the <see cref="Type"/> property documentation for further information.
    /// </para>
    /// </item>
    /// <item>
    /// <para>
    /// Instances of classes annotated with <see cref="CachedAttribute"/> will cache themselves for their own children.
    /// Unless specified differently by <see cref="Type"/>, the dependency will be cached using the type <em>at the point where the <see cref="CachedAttribute"/> was declared</em>.
    /// </para>
    /// <para>
    /// Note that while the <see cref="CachedAttribute"/> itself is not inherited, derived class instances will still cache themselves using the base class.
    /// See the examples section of the <see cref="Type"/> property documentation for further information.
    /// </para>
    /// </item>
    /// <item>
    /// If a class implements an interface annotated with <see cref="CachedAttribute"/>, then instances of that class will cache themselves for their own children using the interface type.
    /// As with classes, the <see cref="CachedAttribute"/> is not inherited between interfaces either,
    /// but an instance of a class will cache itself to children using all cacheable interface types that it implements.
    /// </item>
    /// </list>
    /// </remarks>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class CachedAttribute : Attribute
    {
        internal const BindingFlags ACTIVATOR_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        /// <summary>
        /// The type to cache the value as. If null, the type depends on the type of member that the attribute is placed on:
        /// <list type="bullet">
        /// <item>In the case of fields and properties, the attribute will use the concrete/most-derived type of the field/property's value.</item>
        /// <item>In the case of classes and interfaces, the attribute will use the class/interface type on which the <see cref="CachedAttribute"/> was <em>directly placed</em>.</item>
        /// </list>
        /// </summary>
        /// <example>
        /// <para>
        /// In the case of fields and properties, if this value is <see langword="null"/> on the following field definition:
        /// <code>
        /// [Cached]
        /// private BaseType obj = new DerivedType();
        /// </code>
        /// then the cached type will be <c>DerivedType</c>.
        /// </para>
        /// <para>
        /// In the case of classes, given the following structure:
        /// <code>
        /// [Cached]
        /// public class A { }
        ///
        /// public class B : A { }
        /// </code>
        /// the following things will happen:
        /// <list type="bullet">
        /// <item>Instances of <c>A</c> will cache themselves to children using type <c>A</c>.</item>
        /// <item>Instances of <c>B</c> will cache themselves to children using type <c>A</c>.</item>
        /// <item>
        /// Instances of <c>B</c> will <em>not</em> cache themselves to children using type <c>B</c>.
        /// To achieve that effect, the <see cref="CachedAttribute"/> has to be repeated on class <c>B</c>.
        /// </item>
        /// </list>
        /// <see cref="CachedAttribute"/> placed in interface inheritance hierarchies follows analogous rules to the ones described above for classes.
        /// </para>
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
                return (_, existing, _) => existing;

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
