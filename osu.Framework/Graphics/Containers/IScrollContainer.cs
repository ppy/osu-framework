// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Containers
{
    public interface IScrollContainer : IContainer
    {
        /// <summary>
        /// The direction in which scrolling is supported.
        /// </summary>
        Direction ScrollDirection { get; }
    }
}
