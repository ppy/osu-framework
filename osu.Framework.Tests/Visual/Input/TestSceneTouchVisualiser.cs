// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
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
                new SpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "Touch on the screen.",
                    Font = FontUsage.Default.With(size: 20),
                },
                new TouchVisualiser(),
            };
        }

        public class TouchVisualiser : CompositeDrawable
        {
            private readonly Drawable[] drawableTouches = new Drawable[10];

            public TouchVisualiser()
            {
                RelativeSizeAxes = Axes.Both;
            }

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

            protected override bool OnTouchDown(TouchDownEvent e)
            {
                if (IsDisposed)
                    return false;

                var circle = new Circle
                {
                    Alpha = 0.5f,
                    Origin = Anchor.Centre,
                    Size = new Vector2(20),
                    Position = e.Touch.Position,
                    Colour = colourFor(e.Touch.Source),
                };

                AddInternal(circle);
                drawableTouches[(int)e.Touch.Source] = circle;
                return false;
            }

            protected override void OnTouchMove(TouchMoveEvent e)
            {
                if (IsDisposed)
                    return;

                var circle = drawableTouches[(int)e.Touch.Source];
                AddInternal(new FadingCircle(circle));
                circle.Position = e.Touch.Position;
            }

            protected override void OnTouchUp(TouchUpEvent e)
            {
                var circle = drawableTouches[(int)e.Touch.Source];
                circle.FadeOut(200, Easing.OutQuint).Expire();
                drawableTouches[(int)e.Touch.Source] = null;
            }

            private Color4 colourFor(TouchSource source)
            {
                return Color4.FromHsv(new Vector4((float)source / TouchState.MAX_TOUCH_COUNT, 1f, 1f, 1f));
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
