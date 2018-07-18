// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Hierarchically caches dependencies and can inject those automatically into types registered for dependency injection.
    /// </summary>
    public class DependencyContainer : IReadOnlyDependencyContainer
    {
        private readonly ConcurrentDictionary<Type, object> cache = new ConcurrentDictionary<Type, object>();

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
        public void Cache<T>(T instance)
            where T : class
        {
            if (instance == null)　throw new ArgumentNullException(nameof(instance));

            cache[instance.GetType()] = instance;
        }

        /// <summary>
        /// Caches an instance of a type as a type of <typeparamref name="T"/>. This instance will be returned each time you <see cref="Get(Type)"/>.
        /// </summary>
        /// <param name="instance">The instance to cache. Must be or derive from <typeparamref name="T"/>.</param>
        public void CacheAs<T>(T instance)
            where T : class
        {
            cache[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
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
            if (cache.TryGetValue(type, out object ret))
                return ret;
            return parentContainer?.Get(type);
        }

        /// <summary>
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        /// <param name="autoRegister">True if the instance should be automatically registered as injectable if it isn't already.</param>
        /// <param name="lazy">True if the dependencies should be initialized lazily.</param>
        public void Inject<T>(T instance, bool autoRegister = true, bool lazy = false)
            where T : class
            => DependencyActivator.Activate(instance, this);
    }
}
