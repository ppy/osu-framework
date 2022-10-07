// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        private readonly Dictionary<CacheInfo, object> cache = new Dictionary<CacheInfo, object>();

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
        public void Cache(object instance)
            => Cache(instance, default);

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="instance">The instance to cache.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        public void Cache(object instance, CacheInfo info)
            => CacheAs(instance.GetType(), info, instance, false);

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        public void CacheAs<T>(T instance) where T : class
            => CacheAs(instance, default);

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        public void CacheAs<T>(T instance, CacheInfo info) where T : class
            => CacheAs(typeof(T), info, instance, false);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        public void CacheAs<T>(Type type, T instance) where T : class
            => CacheAs(type, instance, default);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        public void CacheAs<T>(Type type, T instance, CacheInfo info) where T : class
            => CacheAs(type, info, instance, false);

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).
        /// </remarks>
        /// <param name="instance">The instance to cache.</param>
        internal void CacheValue(object instance)
            => CacheValue(instance, default);

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).
        /// </remarks>
        /// <param name="instance">The instance to cache.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        internal void CacheValue(object instance, CacheInfo info)
        {
            if (instance == null)
                return;

            CacheAs(instance.GetType(), info, instance, true);
        }

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).
        /// </remarks>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        internal void CacheValueAs<T>(T instance)
            => CacheValueAs(instance, default);

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <remarks>
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).
        /// </remarks>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        internal void CacheValueAs<T>(T instance, CacheInfo info)
            => CacheAs(typeof(T), info, instance, true);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        /// <param name="allowValueTypes">Whether value types are allowed to be cached.
        /// This should only be used when it is guaranteed that the internal state of the type will remain consistent through retrieval.
        /// (e.g. <see cref="CancellationToken"/> or reference types).</param>
        internal void CacheAs(Type type, CacheInfo info, object instance, bool allowValueTypes)
        {
            if (instance == null)
            {
                if (allowValueTypes)
                    return;

                throw new ArgumentNullException(nameof(instance));
            }

            info = info.WithType(type.GetUnderlyingNullableType() ?? type);

            var instanceType = instance.GetType();
            instanceType = instanceType.GetUnderlyingNullableType() ?? instanceType;

            if (instanceType.IsValueType && !allowValueTypes)
                throw new ArgumentException($"{instanceType.ReadableName()} must be a class to be cached as a dependency.", nameof(instance));

            if (!info.Type.IsInstanceOfType(instance))
                throw new ArgumentException($"{instanceType.ReadableName()} must be a subclass of {info.Type.ReadableName()}.", nameof(instance));

            // We can theoretically make this work by adding a nested dependency container. That would be a pretty big change though.
            // For now, let's throw an exception as this leads to unexpected behaviours (depends on ordering of processing of attributes vs CreateChildDependencies).
            if (cache.TryGetValue(info, out _))
                throw new TypeAlreadyCachedException(info);

            cache[info] = instance;
        }

        public object Get(Type type)
            => Get(type, default);

        public object Get(Type type, CacheInfo info)
        {
            info = info.WithType(type.GetUnderlyingNullableType() ?? type);

            if (cache.TryGetValue(info, out object existing))
                return existing;

            return parentContainer?.Get(type, info);
        }

        /// <summary>
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        /// <exception cref="OperationCanceledException">When the injection process was cancelled.</exception>
        public void Inject<T>(T instance)
            where T : class
            => DependencyActivator.Activate(instance, this);
    }

    public class TypeAlreadyCachedException : InvalidOperationException
    {
        public TypeAlreadyCachedException(CacheInfo info)
            : base($"An instance of the member ({info.ToString()}) has already been cached to the dependency container.")
        {
        }
    }
}
