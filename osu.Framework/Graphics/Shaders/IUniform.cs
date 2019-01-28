﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Shaders
{
    /// <summary>
    /// Represents an updateable shader uniform.
    /// </summary>
    internal interface IUniform
    {
        void Update();

        Shader Owner { get; }
        int Location { get; }
        string Name { get; }
    }
}
