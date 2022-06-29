// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Visual.Containers
{
    [System.ComponentModel.Description(@"Checking for bugged corner radius")]
    public class TestSceneCircularContainer : FrameworkTestScene
    {
        private SingleUpdateCircularContainer container;

        public TestSceneCircularContainer()
        {
            AddStep("128x128 box", () => addContainer(new Vector2(128)));
            AddAssert("Expect CornerRadius = 64", () => Precision.AlmostEquals(container.CornerRadius, 64));
            AddStep("128x64 box", () => addContainer(new Vector2(128, 64)));
            AddAssert("Expect CornerRadius = 32", () => Precision.AlmostEquals(container.CornerRadius, 32));
            AddStep("64x128 box", () => addContainer(new Vector2(64, 128)));
            AddAssert("Expect CornerRadius = 32", () => Precision.AlmostEquals(container.CornerRadius, 32));
        }

        private void addContainer(Vector2 size)
        {
            Clear();
            Add(container = new SingleUpdateCircularContainer
            {
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Child = new Box { Size = size }
            });
        }

        private class SingleUpdateCircularContainer : CircularContainer
        {
            private bool firstUpdate = true;

            public override bool UpdateSubTree()
            {
                if (!firstUpdate)
                    return true;

                firstUpdate = false;

                return base.UpdateSubTree();
            }
        }
    }
}
