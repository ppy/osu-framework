// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    // TODO: blocking event testing
    public class TestSuiteInputQueueChange : ManualInputManagerTestSuite<TestSceneInputQueueChange>
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            foreach (var b in TestScene.Children.OfType<TestSceneInputQueueChange.HittableBox>())
                b.Reset();
        }

        [Test]
        public void SeparateClicks()
        {
            AddStep("move", () => InputManager.MoveMouseTo(InputManager.Children.First().ScreenSpaceDrawQuad.Centre));
            AddStep("press 1", () => InputManager.PressButton(MouseButton.Button1));
            AddStep("press 2", () => InputManager.PressButton(MouseButton.Button2));
            AddStep("release 1", () => InputManager.ReleaseButton(MouseButton.Button1));
            AddStep("release 2", () => InputManager.ReleaseButton(MouseButton.Button2));
            AddAssert("box 1 was pressed", () => TestScene.Box1.HitCount == 1);
            AddAssert("box 2 was pressed", () => TestScene.Box2.HitCount == 1);
            AddAssert("box 3 not pressed", () => TestScene.Box3.HitCount == 0);
        }

        [Test]
        public void CombinedClicks()
        {
            AddStep("move", () => InputManager.MoveMouseTo(Children.First().ScreenSpaceDrawQuad.Centre));
            AddStep("press 1+2", () =>
            {
                InputManager.PressButton(MouseButton.Button1);
                InputManager.PressButton(MouseButton.Button2);
            });
            AddStep("release 1+2", () =>
            {
                InputManager.ReleaseButton(MouseButton.Button1);
                InputManager.ReleaseButton(MouseButton.Button2);
            });
            AddAssert("box 1 was pressed", () => TestScene.Box1.HitCount == 1);
            AddAssert("box 2 was pressed", () => TestScene.Box2.HitCount == 1);
            AddAssert("box 3 not pressed", () => TestScene.Box3.HitCount == 0);
        }
    }
}
