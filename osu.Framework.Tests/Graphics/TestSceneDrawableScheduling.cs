// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Graphics
{
    [HeadlessTest]
    public class TestSceneDrawableScheduling : FrameworkTestScene
    {
        /// <summary>
        /// Ensures scheduled delegates between <see cref="Drawable.Schedule"/> and <see cref="Scheduler.Add(Action, bool)"/> with no delays execute in correct order.
        /// </summary>
        [Test]
        public void TestExecutionOrder([Values] bool afterChildren)
        {
            Container container = null;
            bool firstExecutedInOrder = false;
            bool secondExecutedInOrder = false;

            AddStep("load container", () => Child = container = new Container());
            AddStep("schedule delegates", () =>
            {
                if (!afterChildren)
                {
                    container.Schedule(() => firstExecutedInOrder = true);
                    container.Scheduler.Add(() => secondExecutedInOrder = firstExecutedInOrder);
                }
                else
                {
                    container.ScheduleAfterChildren(() => firstExecutedInOrder = true);
                    container.SchedulerAfterChildren.Add(() => secondExecutedInOrder = firstExecutedInOrder);
                }
            });

            AddAssert("second executed after first", () => secondExecutedInOrder);
        }
    }
}
