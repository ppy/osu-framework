// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class ToggleStepButton : StepButton
    {
        private readonly Action<bool> reloadCallback;
        private static readonly Colour4 off_colour = Colour4.Red;
        private static readonly Colour4 on_colour = Colour4.YellowGreen;

        public bool State;

        public override int RequiredRepetitions => 2;

        public ToggleStepButton(Action<bool> reloadCallback)
        {
            this.reloadCallback = reloadCallback;
            Action = clickAction;
            LightColour = off_colour;
        }

        private void clickAction()
        {
            State = !State;
            Light.FadeColour(State ? on_colour : off_colour);
            reloadCallback?.Invoke(State);

            if (!State)
                Success();
        }

        public override string ToString() => $"Toggle: {base.ToString()} ({(State ? "on" : "off")})";
    }
}
