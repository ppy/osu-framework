// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using osu.Framework.Graphics.Transforms;

namespace osu.Framework.Graphics.Containers
{
    public interface IFillFlowContainer : ITransformable
    {
        Vector2 Spacing { get; set; }
    }
}
