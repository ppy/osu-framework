// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Transforms;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    public interface IFillFlowContainer : ITransformable
    {
        Vector2 Spacing { get; set; }
    }
}
