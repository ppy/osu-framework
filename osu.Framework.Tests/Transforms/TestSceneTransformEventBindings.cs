// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Tests.Transforms
{
    [HeadlessTest]
    public class TestSceneTransformEventBindings : FrameworkTestScene
    {
        [Test]
        public void TestOnCompleteBinding()
        {
            Container container;
            int completedFired = 0;

            AddStep("setup", () =>
            {
                Child = container = new Container();

                completedFired = 0;
                container.FadeIn(500).Then().FadeOut(500).OnComplete(_ => completedFired++);
            });

            AddAssert("not immediately fired", () => completedFired == 0);
            AddUntilStep("wait for single fire", () => completedFired == 1);
        }

        [Test]
        public void TestOnCompleteBindingImmediateExecution()
        {
            Container container;
            int completedFired = 0;

            AddStep("setup", () =>
            {
                Child = container = new Container();

                completedFired = 0;
                container.FadeIn(500).Then().FadeOut().OnComplete(_ => { completedFired++; });
            });

            AddAssert("not immediately fired", () => completedFired == 0);
            AddUntilStep("wait for single fire", () => completedFired == 1);
        }

        [Test]
        public void TestOnCompleteBindingInterrupted()
        {
            Container container;
            int completedFired = 0;
            int abortFired = 0;

            AddStep("setup", () =>
            {
                Child = container = new Container();

                completedFired = 0;
                abortFired = 0;

                container.FadeIn(500).Then().FadeOut(500).OnAbort(_ => abortFired++);
                container.FadeIn(500).OnComplete(_ => completedFired++);
            });

            AddAssert("not immediately fired", () => completedFired == 0);
            AddAssert("abort fired", () => abortFired == 1);
            AddUntilStep("wait for single fire", () => completedFired == 1);
        }

        [Test]
        public void TestOnCompleteBindingFinishTransforms()
        {
            Container container;
            int completedFired = 0;

            AddStep("setup", () =>
            {
                Child = container = new Container();

                completedFired = 0;
                container.FadeIn(500).Then().FadeOut(500).OnComplete(_ => completedFired++);
                container.FinishTransforms();
            });

            AddAssert("immediately fired", () => completedFired == 1);
        }
    }
}
