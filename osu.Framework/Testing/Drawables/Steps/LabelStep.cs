// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class LabelStep : StepButton
    {
        protected override Colour4 IdleColour => new Colour4(77, 77, 77, 255);

        protected override Colour4 RunningColour => new Colour4(128, 128, 128, 255);

        public LabelStep()
        {
            Light.Hide();
            Height = 30;
        }
    }
}
