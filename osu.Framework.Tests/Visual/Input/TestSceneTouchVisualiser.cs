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
    public class TestSceneTouchVisualiser : ManualInputManagerTestScene
    {
        public TestSceneTouchVisualiser()
        {
            Children = new Drawable[]
            {
                new TouchVisualiser(),
                new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "Touch on the screen.",
                    Font = FontUsage.Default.With(size: 20),
                },
            };
        }

        public class TouchVisualiser : CompositeDrawable
        {
            private static readonly Color4[] colours =
            {
                Color4.Red,
                Color4.Orange,
                Color4.Yellow,
                Color4.Lime,
                Color4.Green,
                Color4.Cyan,
                Color4.Blue,
                Color4.Purple,
                Color4.Magenta,
            };

            private readonly Drawable[] drawableTouches = new Drawable[10];

            public TouchVisualiser()
            {
                Depth = float.NegativeInfinity;
                RelativeSizeAxes = Axes.Both;
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnTouchDown(TouchDownEvent e)
            {
                var circle = new Circle
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(20),
                    Position = e.Touch.Position,
                    Colour = colours[(int)e.Touch.Source]
                };

                AddInternal(circle);
                drawableTouches[(int)e.Touch.Source] = circle;
                return false;
            }

            protected override bool OnTouchMove(TouchMoveEvent e)
            {
                var circle = drawableTouches[(int)e.Touch.Source];
                AddInternal(new FadingCircle(circle));
                circle.Position = e.Touch.Position;
                return false;
            }

            protected override void OnTouchUp(TouchUpEvent e)
            {
                var circle = drawableTouches[(int)e.Touch.Source];
                circle.FadeOut(200, Easing.OutQuint).Expire();
                drawableTouches[(int)e.Touch.Source] = null;
            }

            private class FadingCircle : Circle
            {
                public FadingCircle(Drawable source)
                {
                    Origin = Anchor.Centre;
                    Size = source.Size;
                    Position = source.Position;
                    Colour = source.Colour;
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    this.FadeOut(200).Expire();
                }
            }
        }
    }
}
