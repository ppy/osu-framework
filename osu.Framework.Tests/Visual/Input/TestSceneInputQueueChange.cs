// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneInputQueueChange : ManualInputManagerTestScene
    {
        private readonly HittableBox box1;
        private readonly HittableBox box2;
        private readonly HittableBox box3;

        public TestSceneInputQueueChange()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                box3 = new HittableBox(3),
                box2 = new HittableBox(2),
                box1 = new HittableBox(1),
            };

            // TODO: blocking event testing
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            foreach (var b in Children.OfType<HittableBox>())
                b.Reset();
        });

        [Test]
        public void SeparateClicks()
        {
            AddStep("move", () => InputManager.MoveMouseTo(InputManager.Children.First().ScreenSpaceDrawQuad.Centre));
            AddStep("press 1", () => InputManager.PressButton(MouseButton.Button1));
            AddStep("press 2", () => InputManager.PressButton(MouseButton.Button2));
            AddStep("release 1", () => InputManager.ReleaseButton(MouseButton.Button1));
            AddStep("release 2", () => InputManager.ReleaseButton(MouseButton.Button2));
            AddAssert("box 1 was pressed", () => box1.HitCount == 1);
            AddAssert("box 2 was pressed", () => box2.HitCount == 1);
            AddAssert("box 3 not pressed", () => box3.HitCount == 0);
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
            AddAssert("box 1 was pressed", () => box1.HitCount == 1);
            AddAssert("box 2 was pressed", () => box2.HitCount == 1);
            AddAssert("box 3 not pressed", () => box3.HitCount == 0);
        }

        private class HittableBox : CompositeDrawable
        {
            private readonly int index;

            public int HitCount;

            private float xPos => index * 10;

            public HittableBox(int index)
            {
                this.index = index;
                Position = new Vector2(xPos);
                Size = new Vector2(50);
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                BorderColour = Color4.BlueViolet;
                BorderThickness = 3;
                Masking = true;

                InternalChildren = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Colour = Color4.Black,
                        Text = index.ToString(),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                HitCount++;
                this.MoveToX(xPos + 100).Then().MoveToX(xPos, 1000, Easing.In);
                return true;
            }

            public void Reset()
            {
                FinishTransforms();
                HitCount = 0;
            }
        }
    }
}
