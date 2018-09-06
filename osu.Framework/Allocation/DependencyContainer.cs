// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Hierarchically caches dependencies and can inject those automatically into types registered for dependency injection.
    /// </summary>
    public class DependencyContainer : IReadOnlyDependencyContainer
    {
        private readonly Dictionary<Type, object> cache = new Dictionary<Type, object>();

        private readonly IReadOnlyDependencyContainer parentContainer;

        /// <summary>
        /// Create a new DependencyContainer instance.
        /// </summary>
        /// <param name="parent">An optional parent container which we should use as a fallback for cache lookups.</param>
        public DependencyContainer(IReadOnlyDependencyContainer parent = null)
        {
            parentContainer = parent;
        }

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="instance">The instance to cache.</param>
        public void Cache<T>(T instance) where T : class
            => CacheAs(instance.GetType(), instance, false);

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        public void CacheAs<T>(T instance) where T : class
            => CacheAs(typeof(T), instance, false);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        public void CacheAs<T>(Type type, T instance) where T : class
            => CacheAs(type, instance, false);

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="DependencyContainer.Get(Type)"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).
        /// </remarks>
        /// <param name="instance">The instance to cache.</param>
        internal void CacheValue<T>(T instance)
        {
            if (instance == null)
                return;
            CacheAs(instance.GetType(), instance, true);
        }

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="DependencyContainer.Get(Type)"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).
        /// </remarks>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        internal void CacheValueAs<T>(T instance) => CacheAs(typeof(T), instance, true);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        /// <param name="allowValueTypes">Whether value types are allowed to be cached.
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).</param>
        internal void CacheAs(Type type, object instance, bool allowValueTypes)
        {
            if (instance == null)
            {
                if (allowValueTypes)
                    return;
                throw new ArgumentNullException(nameof(instance));
            }

            type = Nullable.GetUnderlyingType(type) ?? type;

            var instanceType = instance.GetType();
            instanceType = Nullable.GetUnderlyingType(instanceType) ?? instanceType;

            if (instanceType.IsValueType && !allowValueTypes)
                throw new ArgumentException($"{instanceType.ReadableName()} must be a class to be cached as a dependency.", nameof(instance));

            if (!type.IsInstanceOfType(instance))
                throw new ArgumentException($"{instanceType.ReadableName()} must be a subclass of {type.ReadableName()}.", nameof(instance));

            // We can theoretically make this work by adding a nested dependency container. That would be a pretty big change though.
            // For now, let's throw an exception as this leads to unexpected behaviours (depends on ordering of processing of attributes vs CreateChildDependencies).
            if (cache.ContainsKey(type))
                throw new TypeAlreadyCachedException(type);

            cache[type] = instance;
        }

        /// <summary>
        /// Retrieves a cached dependency of <paramref name="type"/> if it exists. If not, then the parent
        /// <see cref="IReadOnlyDependencyContainer"/> is recursively queried. If no parent contains
        /// <paramref name="type"/>, then null is returned.
        /// </summary>
        /// <param name="type">The dependency type to query for.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        public object Get(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (cache.TryGetValue(type, out object ret))
                return ret;
            return parentContainer?.Get(type);
        }

        /// <summary>
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        /// <exception cref="DependencyInjectionException">When any user error has occurred.
        /// Rethrow <see cref="DependencyInjectionException.DispatchInfo"/> when appropriate to retrieve the original exception.</exception>
        /// <exception cref="OperationCanceledException">When the injection process was cancelled.</exception>
        public void Inject<T>(T instance)
            where T : class
            => DependencyActivator.Activate(instance, this);
    }

    public class TypeAlreadyCachedException : InvalidOperationException
    {
        public TypeAlreadyCachedException(Type type)
            : base($"An instance of type {type.ReadableName()} has already been cached to the dependency container.")
        {
        }
    }
}
