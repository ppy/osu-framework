// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.UserInterface;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.StepButtons
{
    public class StepButton : Button
    {
        public virtual int RequiredRepetitions => 1;

        public StepButton()
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
    }
}