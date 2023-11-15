// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual
{
    public partial class TestScenePerspectiveContainer : FrameworkTestScene
    {
        public TestScenePerspectiveContainer()
        {
            Add(new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f),
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                ExtraRotation = Matrix3.CreateRotationX(MathF.PI / 2.5f)
            });
        }
    }
}
