// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class LabelStep : StepButton
    {
        protected override Color4 IdleColour => new Color4(77, 77, 77, 255);

        protected override Color4 RunningColour => new Color4(128, 128, 128, 255);

        public LabelStep()
        {
            Light.Hide();
            Height = 30;
        }
    }
}
