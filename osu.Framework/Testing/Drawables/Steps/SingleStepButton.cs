// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class SingleStepButton : StepButton
    {
        public new Action Action;

        public SingleStepButton(bool isSetupStep = false)
            : base(isSetupStep)
        {
            base.Action = () =>
            {
                Action?.Invoke();
                Success();
            };
        }
    }
}
