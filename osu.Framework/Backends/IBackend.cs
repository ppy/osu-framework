// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Backends
{
    /// <summary>
    /// All backends inherit from <see cref="IBackend"/> to define them as <see cref="IDisposable"/>
    /// and provide an initialisation method that is executed after all backends have been created.
    /// </summary>
    public interface IBackend : IDisposable
    {
        /// <summary>
        /// Performs initialisation of the backend. Backends provided by the passed <see cref="IGameHost"/>
        /// will never be null, but there is no guarantee they have been <see cref="Initialise"/>d.
        /// </summary>
        /// <param name="host">The calling <see cref="IGameHost"/>.</param>
        void Initialise(IGameHost host);
    }
}
