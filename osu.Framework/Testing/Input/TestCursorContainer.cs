// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Testing.Input
{
    internal class TestCursorContainer : CursorContainer
    {
        protected override Drawable CreateCursor() => new TestCursor();
    }

    internal class TestCursor : CompositeDrawable
    {
        private readonly Container circle;

        private readonly Container border;

        public readonly Container Left;
        public readonly Container Right;

        public override bool PropagatePositionalInputSubTree => true;

        public TestCursor()
        {
            Size = new Vector2(30);

            Origin = Anchor.Centre;

            InternalChildren = new Drawable[]
            {
                Left = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Alpha = 0,
                    Width = 0.5f,
                    Child = new CircularContainer
                    {
                        Size = new Vector2(30),
                        Masking = true,
                        BorderThickness = 5,
                        BorderColour = Color4.Cyan,
                        Child = new Box
                        {
                            Colour = Color4.Black,
                            Alpha = 0.1f,
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                },
                Right = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Alpha = 0,
                    Width = 0.5f,
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Child = new CircularContainer
                    {
                        Size = new Vector2(30),
                        X = -15,
                        Masking = true,
                        BorderThickness = 5,
                        BorderColour = Color4.Cyan,
                        Child = new Box
                        {
                            Colour = Color4.Black,
                            Alpha = 0.1f,
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                },
                border = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 2,
                    BorderColour = Color4.Cyan,
                    Child = new Box
                    {
                        Colour = Color4.Black,
                        Alpha = 0.1f,
                        RelativeSizeAxes = Axes.Both,
                    },
                },
                circle = new CircularContainer
                {
                    Size = new Vector2(8),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    BorderThickness = 2,
                    BorderColour = Color4.White,
                    Child = new Box
                    {
                        Colour = Color4.Red,
                        RelativeSizeAxes = Axes.Both,
                    },
                },
            };
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    Left.FadeIn();
                    break;

                case MouseButton.Right:
                    Right.FadeIn();
                    break;
            }

            updateBorder(e);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            switch (e.Button)
            {
                case MouseButton.Left:
                    Left.FadeOut(500);
                    break;

                case MouseButton.Right:
                    Right.FadeOut(500);
                    break;
            }

            updateBorder(e);
            base.OnMouseUp(e);
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            circle.MoveTo(circle.Position - e.ScrollDelta * 10).MoveTo(Vector2.Zero, 500, Easing.OutQuint);
            return base.OnScroll(e);
        }

        private void updateBorder(MouseButtonEvent e)
        {
            border.BorderColour = e.CurrentState.Mouse.Buttons.Any() ? Color4.Red : Color4.Cyan;
        }
    }
}
