// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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

        private Color4 lightColour = Color4.BlueViolet;

        public Color4 LightColour
        {
            get { return lightColour; }
            set
            {
                lightColour = value;
                if (IsLoaded) Reset();
            }
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

            BorderThickness = 1.5f;
            BorderColour = new Color4(0.15f, 0.15f, 0.15f, 1);

            CornerRadius = 2;
            Masking = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Reset();
        }

        protected override bool OnClick(InputState state)
        {
            try
            {
                PerformStep(true);
            }
            catch (Exception e)
            {
                Logging.Logger.Error(e, $"Step {this} triggered an error");
            }

            return true;
        }

        /// <summary>
        /// Reset this step to a default state.
        /// </summary>
        public virtual void Reset()
        {
            Background.DelayUntilTransformsFinished().FadeColour(idleColour, 1000, Easing.OutQuint);
            Light.FadeColour(lightColour);
        }

        public virtual void PerformStep(bool userTriggered = false)
        {
            Background.ClearTransforms();
            Background.FadeColour(runningColour, 400, Easing.OutQuint);

            try
            {
                Action?.Invoke();
            }
            catch (Exception)
            {
                Failure();
                throw;
            }
        }

        protected virtual void Failure()
        {
            Background.DelayUntilTransformsFinished().FadeColour(new Color4(0.3f, 0.15f, 0.15f, 1), 1000, Easing.OutQuint);
            Light.FadeColour(Color4.Red);
        }

        protected virtual void Success()
        {
            Background.FinishTransforms();
            Background.FadeColour(idleColour, 1000, Easing.OutQuint);

            Light.FadeColour(Color4.YellowGreen);
            SpriteText.Alpha = 0.8f;
        }

        public override string ToString() => Text;
    }
}
