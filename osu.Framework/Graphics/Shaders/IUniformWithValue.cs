// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics.Shaders
{
    public interface IUniformWithValue<T> : IUniform
        where T : unmanaged, IEquatable<T>
    {
        /// <summary>
        /// Returns a reference to the current value of this uniform.
        /// </summary>
        ref T GetValueByRef();

        /// <summary>
        /// Returns the current value of this uniform.
        /// </summary>
        T GetValue();
    }
}
