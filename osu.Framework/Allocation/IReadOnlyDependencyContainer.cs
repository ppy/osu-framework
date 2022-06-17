// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Read-only interface into a dependency container capable of injective and retrieving dependencies based
    /// on types.
    /// </summary>
    public interface IReadOnlyDependencyContainer
    {
        /// <summary>
        /// Retrieves a cached dependency of <paramref name="type"/> if it exists and null otherwise.
        /// </summary>
        /// <param name="type">The dependency type to query for.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        object Get(Type type);

        /// <summary>
        /// Retrieves a cached dependency of <paramref name="type"/> if it exists and null otherwise.
        /// </summary>
        /// <param name="type">The dependency type to query for.</param>
        /// <param name="info">Extra information that identifies the cached dependency.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        object Get(Type type, CacheInfo info);

        /// <summary>
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        void Inject<T>(T instance) where T : class;
    }

    public static class ReadOnlyDependencyContainerExtensions
    {
        /// <summary>
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists, and null otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        public static T Get<T>(this IReadOnlyDependencyContainer container)
            where T : class
            => Get<T>(container, default);

        /// <summary>
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists, and null otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <param name="info">Extra information that identifies the cached dependency.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        public static T Get<T>(this IReadOnlyDependencyContainer container, CacheInfo info)
            where T : class
            => (T)container.Get(typeof(T), info);

        /// <summary>
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists, and default(<typeparamref name="T"/>) otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <returns>The requested dependency, or default(<typeparamref name="T"/>) if not found.</returns>
        internal static T GetValue<T>(this IReadOnlyDependencyContainer container)
            => GetValue<T>(container, default);

        /// <summary>
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists, and default(<typeparamref name="T"/>) otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <param name="info">Extra information that identifies the cached dependency.</param>
        /// <returns>The requested dependency, or default(<typeparamref name="T"/>) if not found.</returns>
        internal static T GetValue<T>(this IReadOnlyDependencyContainer container, CacheInfo info)
        {
            if (container.Get(typeof(T), info) is T value)
                return value;

            return default;
        }

        /// <summary>
        /// Tries to retrieve a cached dependency of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <param name="value">The requested dependency, or null if not found.</param>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <returns>Whether the requested dependency existed.</returns>
        public static bool TryGet<T>(this IReadOnlyDependencyContainer container, out T value)
            where T : class
            => TryGet(container, out value, default);

        /// <summary>
        /// Tries to retrieve a cached dependency of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <param name="value">The requested dependency, or null if not found.</param>
        /// <param name="info">Extra information that identifies the cached dependency.</param>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <returns>Whether the requested dependency existed.</returns>
        public static bool TryGet<T>(this IReadOnlyDependencyContainer container, out T value, CacheInfo info)
            where T : class
        {
            value = container.Get<T>(info);
            return value != null;
        }
    }
}
