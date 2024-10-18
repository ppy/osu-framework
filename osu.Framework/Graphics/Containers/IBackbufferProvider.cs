// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which ensures that its children are drawn to a framebuffer.
    /// </summary>
    [Cached]
    public interface IBackbufferProvider : IContainer
    {
    }
}
