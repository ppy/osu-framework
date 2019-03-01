// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Shaders
{
    internal interface IUniformWithValue<T> : IUniform
        where T : struct
    {
        ref T GetValueByRef();
        T GetValue();
    }
}
