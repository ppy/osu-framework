// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSuiteCachedBufferedContainer : TestSuite<TestSceneCachedBufferedContainer>
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BufferedContainer),
        };

        public TestSuiteCachedBufferedContainer()
        {
            AddWaitStep("wait for boxes", 5);

            // ensure uncached is always updating children.
            AddAssert("box 0 count > 0", () => TestScene.Boxes[0].Count > 0);
            AddAssert("even box counts equal", () =>
                TestScene.Boxes[0].Count == TestScene.Boxes[2].Count &&
                TestScene.Boxes[2].Count == TestScene.Boxes[4].Count &&
                TestScene.Boxes[4].Count == TestScene.Boxes[6].Count);

            // ensure cached is never updating children.
            AddAssert("box 1 count is 1", () => TestScene.Boxes[1].Count == 1);

            // ensure rotation changes are invalidating cache (for now).
            AddAssert("box 2 count > 0", () => TestScene.Boxes[2].Count > 0);
            AddAssert("box 3 count is less than box 2 count", () => TestScene.Boxes[3].Count < TestScene.Boxes[2].Count);

            // ensure cached with only translation is never updating children.
            AddAssert("box 5 count is 1", () => TestScene.Boxes[1].Count == 1);

            // ensure a parent scaling is invalidating cache.
            AddAssert("box 5 count is less than box 6 count", () => TestScene.Boxes[5].Count < TestScene.Boxes[6].Count);

            // ensure we don't break on colour invalidations (due to blanket invalidation logic in Drawable.Invalidate).
            AddAssert("box 7 count equals box 8 count", () => TestScene.Boxes[7].Count == TestScene.Boxes[8].Count);

            AddAssert("box 10 count is 1", () => TestScene.Boxes[10].Count == 1);
        }
    }
}
