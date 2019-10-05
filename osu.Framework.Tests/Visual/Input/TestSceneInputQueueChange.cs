// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneInputQueueChange : TestScene
    {
        public readonly HittableBox Box1;
        public readonly HittableBox Box2;
        public readonly HittableBox Box3;

        public TestSceneInputQueueChange()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                Box3 = new HittableBox(3),
                Box2 = new HittableBox(2),
                Box1 = new HittableBox(1),
            };
        }

        public class HittableBox : CompositeDrawable
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
