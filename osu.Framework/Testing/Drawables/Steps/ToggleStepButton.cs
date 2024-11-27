// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public partial class ToggleStepButton : StepButton
    {
        private static readonly Color4 off_colour = Color4.Red;
        private static readonly Color4 on_colour = Color4.YellowGreen;

        public new required Action<bool> Action { get; init; }

        public override int RequiredRepetitions => 2;

        private bool state;

        public ToggleStepButton()
        {
            base.Action = clickAction;
            LightColour = off_colour;
        }

        private void clickAction()
        {
            state = !state;
            Light.FadeColour(state ? on_colour : off_colour);
            Action(state);

            if (!state)
                Success();
        }

        public override string ToString() => $"Toggle: {base.ToString()} ({(state ? "on" : "off")})";
    }
}
