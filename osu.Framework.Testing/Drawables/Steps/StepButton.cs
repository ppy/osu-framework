// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public abstract class StepButton : Button
    {
        public virtual int RequiredRepetitions => 1;

        protected StepButton()
        {
            Height = 25;
            RelativeSizeAxes = Axes.X;

            BackgroundColour = Color4.BlueViolet;

            CornerRadius = 2;
            Masking = true;

            SpriteText.Anchor = Anchor.CentreLeft;
            SpriteText.Origin = Anchor.CentreLeft;
            SpriteText.Padding = new MarginPadding(5);
        }

        protected void Success()
        {
            Background.Alpha = 0.4f;
            SpriteText.Alpha = 0.8f;
        }

        public override string ToString() => Text;
    }
}