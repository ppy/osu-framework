// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// An object referenced via <see cref="ResourceAllocator"/>.
    /// </summary>
    /// <param name="Id">The object identifier.</param>
    internal readonly record struct ResourceReference(int Id);
}
