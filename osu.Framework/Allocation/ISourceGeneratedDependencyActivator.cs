// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Allocation
{
    /// <summary>
    /// Implemented by source generators to inject dependencies into an object.
    /// </summary>
    /// <remarks>
    /// Do not manually implement this interface.
    /// </remarks>
    public interface ISourceGeneratedDependencyActivator
    {
        /// <summary>
        /// Invoked to register dependency activation functions for this object.
        /// </summary>
        /// <param name="registry">The <see cref="IDependencyActivatorRegistry"/> to register activation functions with.</param>
        void RegisterForDependencyActivation(IDependencyActivatorRegistry registry);
    }
}
