// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Tests.Visual.Sprites
{
    public partial class TestSceneSpriteTextSizing : FrameworkTestScene
    {
        [Test]
        public void TestNewSizeImmediatelyAvailableAfterTextChange()
        {
            SpriteText text = null!;
            float initialSize = 0;

            AddStep("add initial text and get size", () =>
            {
                Child = text = new SpriteText { Text = "First" };
                initialSize = text.DrawWidth;
            });

            float updatedSize = 0;
            AddStep("set new text and grab size", () =>
            {
                text.Text = "Second";
                updatedSize = text.DrawWidth;
            });

            AddAssert("updated size is not equal to the initial size", () => updatedSize, () => Is.Not.EqualTo(initialSize));
        }
    }
}
