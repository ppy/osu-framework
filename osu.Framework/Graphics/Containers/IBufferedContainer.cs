// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    [Cached]
    public interface IBufferedContainer : IContainer
    {
        Vector2 BlurSigma { get; set; }

        float GrayscaleStrength { get; set; }
    }
}
