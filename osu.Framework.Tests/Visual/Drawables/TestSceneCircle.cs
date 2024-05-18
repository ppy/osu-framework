// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneCircle : FrameworkTestScene
    {
        private readonly FastCircle circle;

        public TestSceneCircle()
        {
            Add(circle = new FastCircle
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(100)
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddSliderStep("Width", 0, 400, 100, w => circle.Width = w);
            AddSliderStep("Height", 0, 400, 100, h => circle.Height = h);
        }
    }
}
