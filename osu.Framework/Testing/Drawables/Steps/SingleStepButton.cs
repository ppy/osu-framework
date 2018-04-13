// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class SingleStepButton : StepButton
    {
        public new Action Action;

        public SingleStepButton()
        {
            base.Action = () =>
            {
                Action?.Invoke();
                Success();
            };
        }
    }
}
