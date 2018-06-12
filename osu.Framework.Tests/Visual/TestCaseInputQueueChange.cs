// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseInputQueueChange : TestCase
    {
        private readonly ManualInputManager manual;

        private readonly HittableBox box1;
        private readonly HittableBox box2;
        private readonly HittableBox box3;

        public TestCaseInputQueueChange()
        {
            Children = new Drawable[]
            {
                manual = new ManualInputManager
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        box3 = new HittableBox(3),
                        box2 = new HittableBox(2),
                        box1 = new HittableBox(1),
                    }
                },
            };

            AddStep("return input", () => manual.UseParentState = true);

            // TODO: blocking event testing
        }

        [SetUp]
        public void SetUp()
        {
            // grab manual input control
            manual.UseParentState = false;
            foreach (var b in manual.Children.OfType<HittableBox>())
                b.Reset();
        }

        [Test]
        public void SeparateClicks()
        {
            AddStep("move", () => manual.MoveMouseTo(manual.Children.First().ScreenSpaceDrawQuad.Centre));
            AddStep("press 1", () => manual.PressButton(MouseButton.Button1));
            AddStep("press 2", () => manual.PressButton(MouseButton.Button2));
            AddStep("release 1", () => manual.ReleaseButton(MouseButton.Button1));
            AddStep("release 2", () => manual.ReleaseButton(MouseButton.Button2));
            AddAssert("box 1 was pressed", () => box1.HitCount == 1);
            AddAssert("box 2 was pressed", () => box2.HitCount == 1);
            AddAssert("box 3 not pressed", () => box3.HitCount == 0);
        }

        [Test]
        public void CombinedClicks()
        {
            AddStep("move", () => manual.MoveMouseTo(manual.Children.First().ScreenSpaceDrawQuad.Centre));
            AddStep("press 1+2", () =>
            {
                manual.PressButton(MouseButton.Button1);
                manual.PressButton(MouseButton.Button2);
            });
            AddStep("release 1+2", () =>
            {
                manual.ReleaseButton(MouseButton.Button1);
                manual.ReleaseButton(MouseButton.Button2);
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

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
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
