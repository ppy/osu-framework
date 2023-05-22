// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// An interface for objects to register dependency activation functions with.
    /// </summary>
    public interface IDependencyActivatorRegistry
    {
        /// <summary>
        /// Whether dependency activation functions have already been registered for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        bool IsRegistered(Type type);

        /// <summary>
        /// Registers dependency activation functions for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="injectDel">A function to inject dependencies into the object.</param>
        /// <param name="cacheDel">A function to cache the dependencies of the object.</param>
        void Register(Type type, InjectDependencyDelegate? injectDel, CacheDependencyDelegate? cacheDel);
    }
}
