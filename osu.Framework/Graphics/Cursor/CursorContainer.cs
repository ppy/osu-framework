// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Cursor
{
    public class CursorContainer : VisibilityContainer, IRequireHighFrequencyMousePosition
    {
        public Drawable ActiveCursor { get; protected set; }

        private TouchLongPressFeedback longPressFeedback;

        private InputManager inputManager;

        public CursorContainer()
        {
            Depth = float.MinValue;
            RelativeSizeAxes = Axes.Both;

            State.Value = Visibility.Visible;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(ActiveCursor = CreateCursor());
            Add(longPressFeedback = CreateLongPressFeedback());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager();
            inputManager.TouchLongPressBegan += duration =>
            {
                longPressFeedback.Position = Parent.ToLocalSpace(inputManager.CurrentState.Mouse.Position);
                longPressFeedback.BeginAnimation(duration);
            };
            inputManager.TouchLongPressCancelled += longPressFeedback.CancelAnimation;
        }

        protected virtual Drawable CreateCursor() => new Cursor();

        /// <summary>
        /// Creates a drawable providing visual feedback for touch long-presses, signaled via <see cref="TouchLongPressFeedback.BeginAnimation"/> and <see cref="TouchLongPressFeedback.CancelAnimation"/>.
        /// </summary>
        protected virtual TouchLongPressFeedback CreateLongPressFeedback() => new CircularLongPressFeedback();

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        // make sure we always receive positional input, regardless of our visibility state.
        public override bool PropagatePositionalInputSubTree => true;

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            ActiveCursor.Position = e.MousePosition;
            return base.OnMouseMove(e);
        }

        protected override void PopIn()
        {
            Alpha = 1;
        }

        protected override void PopOut()
        {
            Alpha = 0;
        }

        private class Cursor : CircularContainer
        {
            public Cursor()
            {
                AutoSizeAxes = Axes.Both;
                Origin = Anchor.Centre;

                BorderThickness = 2;
                BorderColour = new Color4(247, 99, 164, 255);

                Masking = true;
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = new Color4(247, 99, 164, 6),
                    Radius = 50
                };

                Child = new Box
                {
                    Size = new Vector2(8, 8),
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                };
            }
        }

        private class CircularLongPressFeedback : TouchLongPressFeedback
        {
            private const Easing fade_easing = Easing.OutQuint;

            private readonly CircularProgress progress;

            public CircularLongPressFeedback()
            {
                AutoSizeAxes = Axes.Both;
                Origin = Anchor.Centre;

                InternalChild = progress = new CircularProgress
                {
                    Size = new Vector2(100),
                    InnerRadius = 0.1f,
                    Colour = Color4.LightGray,
                };
            }

            public override void BeginAnimation(double duration)
                => this.Delay(duration / 3).FadeIn()
                       .TransformBindableTo(progress.Current, 0)
                       .TransformBindableTo(progress.Current, 1, duration * 2 / 3, Easing.InOutSine).Then()
                       .FadeOut(200, Easing.InOutSine);

            public override void CancelAnimation()
                => this.TransformBindableTo(progress.Current, 0, 200, Easing.InOutSine)
                       .FadeOut(200, Easing.InOutSine);
        }
    }
}
