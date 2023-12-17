// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal interface IGLUniformBuffer
    {
        int Id { get; }

        void Flush();
    }
}
