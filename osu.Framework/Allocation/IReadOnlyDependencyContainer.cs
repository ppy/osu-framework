// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        /// Injects dependencies into the given instance.
        /// </summary>
        /// <typeparam name="T">The type of the instance to inject dependencies into.</typeparam>
        /// <param name="instance">The instance to inject dependencies into.</param>
        /// <param name="autoRegister">True if the instance should be automatically registered as injectable if it isn't already.</param>
        /// <param name="lazy">True if the dependencies should be initialized lazily.</param>
        void Inject<T>(T instance, bool autoRegister = true, bool lazy = false) where T : class;
    }

    public static class ReadOnlyDependencyContainerExtensions
    {
        /// <summary>
        /// Retrieves a cached dependency of type <typeparamref name="T"/> if it exists and null otherwise.
        /// </summary>
        /// <typeparam name="T">The dependency type to query for.</typeparam>
        /// <param name="container">The <see cref="IReadOnlyDependencyContainer"/> to query.</param>
        /// <returns>The requested dependency, or null if not found.</returns>
        public static T Get<T>(this IReadOnlyDependencyContainer container) where T : class =>
            (T)container.Get(typeof(T));
    }
}
