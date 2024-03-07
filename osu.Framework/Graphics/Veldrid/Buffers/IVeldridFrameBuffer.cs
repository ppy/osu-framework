// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridFrameBuffer : IFrameBuffer
    {
        Framebuffer Framebuffer { get; }
    }
}
