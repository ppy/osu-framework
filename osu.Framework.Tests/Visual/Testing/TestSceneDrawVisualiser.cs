// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Input;
using osu.Framework.Tests.Visual.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneDrawVisualiser : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            DrawVisualiser vis;
            Drawable target;

            // Avoid stack-overflow scenarios by isolating the hovered drawables through a new input manager
            Child = new PassThroughInputManager
            {
                Children = new[]
                {
                    target = new TestSceneDynamicDepth
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Size = new Vector2(0.5f)
                    },
                    vis = new DrawVisualiser(),
                }
            };

            vis.Show();
            vis.Target = target;
        }
    }
}
