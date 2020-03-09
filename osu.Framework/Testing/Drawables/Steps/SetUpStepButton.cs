// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    /// <summary>
    /// A step which is used to get a test into a ready state. Automatically run by test browser.
    /// </summary>
    internal class SetUpStepButton : SingleStepButton
    {
        public SetUpStepButton()
        {
            Text = "[SetUp]";
            LightColour = Color4.Teal;
        }
    }
}
