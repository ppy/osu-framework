// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Testing.Drawables.Steps
{
    public partial class SingleStepButton : StepButton
    {
        public new required Action Action { get; init; }

        public SingleStepButton()
        {
            base.Action = clickAction;
        }

        private void clickAction()
        {
            Action();
            Success();
        }
    }
}
