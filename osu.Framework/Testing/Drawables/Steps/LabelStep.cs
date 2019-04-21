// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class LabelStep : StepButton
    {
        protected override Color4 IdleColour => Color4.RoyalBlue;

        protected override Color4 RunningColour => Color4.RoyalBlue.Lighten(0.5f);

        public LabelStep()
        {
            LightColour = IdleColour.Darken(0.5f);
        }
    }
}
