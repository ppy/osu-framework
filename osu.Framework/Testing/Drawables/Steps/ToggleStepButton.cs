// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class ToggleStepButton : StepButton
    {
        private readonly Action<bool> reloadCallback;
        private static readonly Color4 off_colour = Color4.Red;
        private static readonly Color4 on_colour = Color4.YellowGreen;

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
