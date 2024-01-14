// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// A buffer which stores an array of data for use in a shader.
    /// </summary>
    /// <typeparam name="TData">The type of data contained.</typeparam>
    public interface IShaderStorageBufferObject<TData> : IUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        /// <summary>
        /// The size of this buffer.
        /// </summary>
        int Size { get; }

        /// <summary>
        /// The data contained by this <see cref="IShaderStorageBufferObject{TData}"/>.
        /// </summary>
        TData this[int index] { get; set; }
    }
}
