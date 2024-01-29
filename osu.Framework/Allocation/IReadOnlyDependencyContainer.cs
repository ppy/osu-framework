// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists, and null otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <returns>The requested dependency, or null if not found.</returns>
        T Get<T>();

        /// <summary>
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists, and null otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <param name="info">Extra information that identifies the cached dependency.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        T Get<T>(CacheInfo info);

        /// <summary>
        /// Tries to retrieve a cached dependency of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The requested dependency, or null if not found.</param>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <returns>Whether the requested dependency existed.</returns>
        bool TryGet<T>(out T value);

        /// <summary>
        /// Tries to retrieve a cached dependency of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The requested dependency, or null if not found.</param>
        /// <param name="info">Extra information that identifies the cached dependency.</param>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <returns>Whether the requested dependency existed.</returns>
        bool TryGet<T>(out T value, CacheInfo info);

        object? Get(Type type);

        object? Get(Type type, CacheInfo info);

        /// <summary>
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        void Inject<T>(T instance) where T : class, IDependencyInjectionCandidate;
    }
}
