// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Rendering
{
    [Flags]
    public enum StateArrayFlags
    {
        None = 0,

        /// <summary>
        /// Calls to <see cref="IRawVertexBuffer{T}.SetLayout"/> will be cached.
        /// </summary>
        VertexLayout = 1 << 0,

        /// <summary>
        /// The bound <see cref="IRawIndexBuffer"/> will be cached.
        /// </summary>
        IndexBuffer = 1 << 1,

        /// <summary>
        /// Known optimisation for OpenGL which uses a vertex array object.
        /// </summary>
        VertexArray = VertexLayout | IndexBuffer
    }
}
