// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// A device buffer retrieved from a <see cref="DeviceBufferPool"/>.
    /// </summary>
    internal interface IPooledDeviceBuffer : IDisposable
    {
        /// <summary>
        /// The <see cref="DeviceBuffer"/>.
        /// </summary>
        DeviceBuffer Buffer { get; }
    }
}
