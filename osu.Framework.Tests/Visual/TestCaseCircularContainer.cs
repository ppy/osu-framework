// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using OpenTK;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description(@"Checking for bugged corner radius")]
    public class TestCaseCircularContainer : TestCase
    {
        private SingleUpdateCircularContainer container;

        public TestCaseCircularContainer()
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
