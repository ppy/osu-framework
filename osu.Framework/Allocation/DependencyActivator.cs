// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Helper class that provides methods to merge dependencies of objects and inject dependencies into objects.
    /// The process of merging/injecting dependencies into objects happens in a "bottom-up" manner from least derived to most derived.
    /// E.g. Drawable -> CompositeDrawable -> Toolbar, etc...
    /// </summary>
    /// <remarks>
    /// Injection of dependencies is ordered into two processes:
    /// <para>1) Inject into properties marked with <see cref="ResolvedAttribute"/>.</para>
    /// 2) Invoke methods marked with <see cref="BackgroundDependencyLoaderAttribute"/>.
    /// </remarks>
    internal class DependencyActivator
    {
        private static readonly ConcurrentDictionary<Type, DependencyActivator> activator_cache = new ConcurrentDictionary<Type, DependencyActivator>();
        private static readonly DependencyActivatorProxy activator_proxy = new DependencyActivatorProxy();

        private readonly List<InjectDependencyDelegate> injectionActivators = new List<InjectDependencyDelegate>();
        private readonly List<CacheDependencyDelegate> buildCacheActivators = new List<CacheDependencyDelegate>();

        static DependencyActivator()
        {
            // Attributes could have been added or removed when using hot-reload.
            HotReloadCallbackReceiver.CompilationFinished += _ => ClearCache();
        }

        // Source generator pathway.
        private DependencyActivator(Type type, InjectDependencyDelegate injectDel, CacheDependencyDelegate cacheDel)
        {
            injectionActivators.Add(injectDel);
            buildCacheActivators.Add(cacheDel);
            activator_cache[type] = this;
        }

        // Reflection pathway.
        private DependencyActivator(Type type)
        {
            injectionActivators.Add(ResolvedAttribute.CreateActivator(type));
            injectionActivators.Add(BackgroundDependencyLoaderAttribute.CreateActivator(type));
            buildCacheActivators.Add(CachedAttribute.CreateActivator(type));
            activator_cache[type] = this;
        }

        /// <summary>
        /// Clears the dependency activator function cache.
        /// </summary>
        public static void ClearCache() => activator_cache.Clear();

        /// <summary>
        /// Injects dependencies from a <see cref="DependencyContainer"/> into an object.
        /// </summary>
        /// <param name="obj">The object to inject the dependencies into.</param>
        /// <param name="dependencies">The dependencies to use for injection.</param>
        public static void Activate<T>(T obj, IReadOnlyDependencyContainer dependencies)
            where T : IDependencyInjectionCandidate
        {
            initialiseSourceGeneratedActivators(obj);
            activateRecursively(obj, dependencies, obj.GetType());

            static void activateRecursively(object obj, IReadOnlyDependencyContainer dependencies, Type currentType)
            {
                if (currentType == typeof(object))
                    return;

                activateRecursively(obj, dependencies, currentType.BaseType);

                foreach (var a in getActivator(currentType).injectionActivators)
                    a(obj, dependencies);
            }
        }

        /// <summary>
        /// Merges existing dependencies with new dependencies from an object into a new <see cref="IReadOnlyDependencyContainer"/>.
        /// </summary>
        /// <param name="obj">The object whose dependencies should be merged into the dependencies provided by <paramref name="dependencies"/>.</param>
        /// <param name="dependencies">The existing dependencies.</param>
        /// <param name="info">Extra information to identify parameters of <paramref name="obj"/> in the cache with.</param>
        /// <returns>A new <see cref="IReadOnlyDependencyContainer"/> if <paramref name="obj"/> provides any dependencies, otherwise <paramref name="dependencies"/>.</returns>
        public static IReadOnlyDependencyContainer MergeDependencies<T>(T obj, IReadOnlyDependencyContainer dependencies, CacheInfo info = default)
            where T : IDependencyInjectionCandidate
        {
            initialiseSourceGeneratedActivators(obj);
            return mergeRecursively(obj, dependencies, info, obj.GetType());

            static IReadOnlyDependencyContainer mergeRecursively(object obj, IReadOnlyDependencyContainer dependencies, CacheInfo info, Type currentType)
            {
                if (currentType == typeof(object))
                    return dependencies;

                dependencies = mergeRecursively(obj, dependencies, info, currentType.BaseType);

                foreach (var a in getActivator(currentType).buildCacheActivators)
                    dependencies = a(obj, dependencies, info);

                return dependencies;
            }
        }

        /// <summary>
        /// Initialises a potential <see cref="ISourceGeneratedDependencyActivator"/> object.
        /// </summary>
        /// <param name="obj">The object to initialise.</param>
        private static void initialiseSourceGeneratedActivators(object obj)
        {
            if (obj is not ISourceGeneratedDependencyActivator sgActivator)
                return;

            if (!activator_cache.ContainsKey(obj.GetType()))
                sgActivator.RegisterForDependencyActivation(activator_proxy);
        }

        private static DependencyActivator getActivator(Type type)
        {
            if (!activator_cache.TryGetValue(type, out var existing))
                return activator_cache[type] = new DependencyActivator(type);

            return existing;
        }

        /// <summary>
        /// A proxy class used to register activation functions for objects implementing <see cref="ISourceGeneratedDependencyActivator"/>.
        /// </summary>
        private class DependencyActivatorProxy : IDependencyActivatorRegistry
        {
            public bool IsRegistered(Type type) => activator_cache.ContainsKey(type);

            public void Register(Type type, InjectDependencyDelegate injectDel, CacheDependencyDelegate cacheDel)
            {
                // The DependencyActivator constructor stores itself to a static dictionary.
                var _ = new DependencyActivator(
                    type,
                    injectDel ?? ((_, _) => { }),
                    cacheDel ?? ((_, d, _) => d));
            }
        }
    }

    /// <summary>
    /// Occurs when multiple <see cref="BackgroundDependencyLoaderAttribute"/>s exist in one object.
    /// </summary>
    public class MultipleDependencyLoaderMethodsException : Exception
    {
        public MultipleDependencyLoaderMethodsException(Type type)
            : base($"The type {type.ReadableName()} has more than one method marked with a {nameof(BackgroundDependencyLoaderAttribute)}."
                   + "Any given type may only have one such method.")
        {
        }
    }

    /// <summary>
    /// Occurs when an object requests the resolution of a dependency, but the dependency doesn't exist.
    /// This is caused by the dependency not being registered by parent <see cref="CompositeDrawable"/> through
    /// <see cref="CompositeDrawable.CreateChildDependencies"/> or <see cref="CachedAttribute"/>.
    /// </summary>
    public class DependencyNotRegisteredException : Exception
    {
        public DependencyNotRegisteredException(Type type, Type requestedType)
            : base($"The type {type.ReadableName()} has a dependency on {requestedType.ReadableName()}, but the dependency is not registered.")
        {
        }
    }

    /// <summary>
    /// Occurs when a dependency-related operation occurred on a member with an unacceptable access modifier.
    /// </summary>
    public abstract class AccessModifierNotAllowedForMemberException : InvalidOperationException
    {
        protected AccessModifierNotAllowedForMemberException(AccessModifier modifier, MemberInfo member, string description)
            : base($"The access modifier(s) [ {modifier.ToString()} ] are not allowed on \"{member.DeclaringType.ReadableName()}.{member.Name}\". {description}")
        {
        }
    }

    /// <summary>
    /// Occurs when attempting to cache a non-private and non-readonly field with an attached <see cref="CachedAttribute"/>.
    /// </summary>
    public class AccessModifierNotAllowedForCachedValueException : AccessModifierNotAllowedForMemberException
    {
        public AccessModifierNotAllowedForCachedValueException(AccessModifier modifier, MemberInfo member)
            : base(modifier, member, $"A field with an attached {nameof(CachedAttribute)} must be private, readonly,"
                                     + " or be an auto-property with a getter and private (or non-existing) setter.")
        {
        }
    }

    /// <summary>
    /// Occurs when a method with an attached <see cref="BackgroundDependencyLoaderAttribute"/> isn't private.
    /// </summary>
    public class AccessModifierNotAllowedForLoaderMethodException : AccessModifierNotAllowedForMemberException
    {
        public AccessModifierNotAllowedForLoaderMethodException(AccessModifier modifier, MemberInfo member)
            : base(modifier, member, $"A method with an attached {nameof(BackgroundDependencyLoaderAttribute)} must be private.")
        {
        }
    }

    /// <summary>
    /// Occurs when the setter of a property with an attached <see cref="ResolvedAttribute"/> isn't private.
    /// </summary>
    public class AccessModifierNotAllowedForPropertySetterException : AccessModifierNotAllowedForMemberException
    {
        public AccessModifierNotAllowedForPropertySetterException(AccessModifier modifier, MemberInfo member)
            : base(modifier, member, $"A property with an attached {nameof(ResolvedAttribute)} must have a private setter.")
        {
        }
    }

    public delegate void InjectDependencyDelegate(object target, IReadOnlyDependencyContainer dependencies);

    public delegate IReadOnlyDependencyContainer CacheDependencyDelegate(object target, IReadOnlyDependencyContainer existingDependencies, CacheInfo info);
}
