// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// A buffer which stores data for a uniform block.
    /// </summary>
    public interface IUniformBuffer : IDisposable
    {
    }

    /// <inheritdoc cref="IUniformBuffer"/>
    /// <typeparam name="TData">The type of data in the buffer.</typeparam>
    public interface IUniformBuffer<TData> : IUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        /// <summary>
        /// The data contained by this <see cref="IUniformBuffer{TData}"/>.
        /// </summary>
        TData Data { get; set; }
    }
}
