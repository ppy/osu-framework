// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Graphics.Shaders
{
    internal interface IUniformWithValue<T> : IUniform
        where T : struct, IEquatable<T>
    {
        ref T GetValueByRef();
        T GetValue();
    }
}
