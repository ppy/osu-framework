// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Platform;
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

            BorderThickness = 1.5f;
            BorderColour = new Color4(0.15f, 0.15f, 0.15f, 1);

            CornerRadius = 2;
            Masking = true;
        }

        private bool interactive;

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            interactive = host.Window != null;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Reset();
        }

        protected override bool OnClick(InputState state)
        {
            PerformStep(true);
            return true;
        }

        /// <summary>
        /// Reset this step to a default state.
        /// </summary>
        public virtual void Reset()
        {
            Background.DelayUntilTransformsFinished().FadeColour(idleColour, 1000, Easing.OutQuint);
            BackgroundColour = Color4.BlueViolet;
        }

        public virtual bool PerformStep(bool userTriggered = false)
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

                // only hard crash on a non-interactive run.
                if (!interactive)
                    throw;

                return false;
            }

            return true;
        }

        protected virtual void Failure()
        {
            Background.DelayUntilTransformsFinished().FadeColour(new Color4(0.3f, 0.15f, 0.15f, 1), 1000, Easing.OutQuint);
            BackgroundColour = Color4.Red;
        }

        protected virtual void Success()
        {
            Background.FinishTransforms();
            Background.FadeColour(idleColour, 1000, Easing.OutQuint);

            BackgroundColour = Color4.YellowGreen;
            SpriteText.Alpha = 0.8f;
        }

        public override string ToString() => Text;
    }
}
