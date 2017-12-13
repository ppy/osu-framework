// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public abstract class StepButton : CompositeDrawable
    {
        public virtual int RequiredRepetitions => 1;

        protected Box Light;
        protected Box Background;
        protected SpriteText SpriteText;

        public Action Action { get; protected set; }

        public string Text
        {
            get { return SpriteText.Text; }
            set { SpriteText.Text = value; }
        }

        public Color4 BackgroundColour
        {
            get { return Light.Colour; }
            set { Light.FadeColour(value); }
        }

        private readonly Color4 idleColour = new Color4(0.15f, 0.15f, 0.15f, 1);
        private readonly Color4 runningColour = new Color4(0.5f, 0.5f, 0.5f, 1);

        protected StepButton()
        {
            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = idleColour,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                Light = new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 5,
                },
                SpriteText = new SpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    TextSize = 14,
                    X = 5,
                    Padding = new MarginPadding(5),
                }
            };

            Height = 20;
            RelativeSizeAxes = Axes.X;

            BackgroundColour = Color4.BlueViolet;

            BorderThickness = 1.5f;
            BorderColour = new Color4(0.15f, 0.15f, 0.15f, 1);

            CornerRadius = 2;
            Masking = true;
        }

        protected override bool OnClick(InputState state)
        {
            Background.ClearTransforms();
            Background.FadeColour(runningColour, 40, Easing.OutQuint);

            try
            {
                Action?.Invoke();
            }
            catch (Exception)
            {
                Failure();

                // if our state is null, we were triggered programmatically and want to handle the exception in the outer scope.
                if (state == null)
                    throw;
            }

            return true;
        }

        protected virtual void Failure()
        {
            Background.DelayUntilTransformsFinished().FadeColour(new Color4(0.3f, 0.15f, 0.15f, 1), 1000, Easing.OutQuint);
        }

        protected virtual void Success()
        {
            Background.DelayUntilTransformsFinished().FadeColour(idleColour, 1000, Easing.OutQuint);
            SpriteText.Alpha = 0.8f;
        }

        public override string ToString() => Text;
    }
}
