// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Hierarchically caches dependencies and can inject those automatically into types registered for dependency injection.
    /// </summary>
    public class DependencyContainer : IReadOnlyDependencyContainer
    {
        private readonly Dictionary<CacheInfo, object> cache = new Dictionary<CacheInfo, object>();

        private readonly IReadOnlyDependencyContainer? parentContainer;

        /// <summary>
        /// Create a new DependencyContainer instance.
        /// </summary>
        /// <param name="parent">An optional parent container which we should use as a fallback for cache lookups.</param>
        public DependencyContainer(IReadOnlyDependencyContainer? parent = null)
        {
            parentContainer = parent;
        }

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="instance">The instance to cache.</param>
        public void Cache(object instance)
            => Cache(instance, default);

        /// <summary>
        /// Caches an instance of a type as its most derived type. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="instance">The instance to cache.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        public void Cache(object instance, CacheInfo info)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            CacheAs(instance.GetType(), info, instance);
        }

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        public void CacheAs<T>(T instance)
            => CacheAs(instance, default);

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        public void CacheAs<T>(T instance, CacheInfo info)
            => CacheAs(typeof(T), info, instance);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        public void CacheAs<T>(Type type, T instance)
            => CacheAs(type, instance, default);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        public void CacheAs<T>(Type type, T instance, CacheInfo info)
            => CacheAs(type, info, instance);

        /// <summary>
        /// Caches an instance of a type as a type of <paramref name="type"/>. This instance will be returned each time you <see cref="Get{T}()"/>.
        /// </summary>
        /// <param name="type">The type to cache <paramref name="instance"/> as.</param>
        /// <param name="info">Extra information to identify <paramref name="instance"/> in the cache.</param>
        /// <param name="instance">The instance to cache. Must be or derive from <paramref name="type"/>.</param>
        internal void CacheAs(Type type, CacheInfo info, object? instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            info = info.WithType(type.GetUnderlyingNullableType() ?? type);

            var instanceType = instance.GetType();
            instanceType = instanceType.GetUnderlyingNullableType() ?? instanceType;

            if (!info.Type.IsInstanceOfType(instance))
                throw new ArgumentException($"{instanceType.ReadableName()} must be a subclass of {info.Type.ReadableName()}.", nameof(instance));

            // We can theoretically make this work by adding a nested dependency container. That would be a pretty big change though.
            // For now, let's throw an exception as this leads to unexpected behaviours (depends on ordering of processing of attributes vs CreateChildDependencies).
            if (cache.TryGetValue(info, out _))
                throw new TypeAlreadyCachedException(info);

            cache[info] = instance;
        }

        public T Get<T>() => Get<T>(default);

        public T Get<T>(CacheInfo info)
        {
            TryGet(out T value, info);
            return value;
        }

        public bool TryGet<T>(out T value) => TryGet(out value, default);

        public bool TryGet<T>(out T value, CacheInfo info)
        {
            object? obj = Get(typeof(T), info);

            if (obj == null)
            {
                // `(int)(object)null` throws a NRE, so `default` is used instead.
                value = default!;
                return false;
            }

            value = (T)obj;
            return true;
        }

        public object? Get(Type type) => Get(type, default);

        public object? Get(Type type, CacheInfo info)
        {
            info = info.WithType(type.GetUnderlyingNullableType() ?? type);

            if (cache.TryGetValue(info, out object? existing))
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
            where T : class, IDependencyInjectionCandidate
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
