// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.UserInterface;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneColorPicker : FrameworkTestScene
    {
        public TestSceneColorPicker()
        {
            var colorPicker = new ColorPicker();
            colorPicker.Current.Value = Color4.Red;
            Add(colorPicker);
        }
    }
}
