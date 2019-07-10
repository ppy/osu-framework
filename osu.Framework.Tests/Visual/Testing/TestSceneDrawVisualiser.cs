// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Visualisation;
using osu.Framework.Tests.Visual.Containers;
using osuTK;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneDrawVisualiser : FrameworkTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(TreeContainer),
            typeof(FlashyBox),
            typeof(IContainVisualisedDrawables),
            typeof(InfoOverlay),
            typeof(LogOverlay),
            typeof(PropertyDisplay),
            typeof(TitleBar),
            typeof(TreeContainer),
            typeof(TreeContainerStatus),
            typeof(VisualisedDrawable),
            typeof(ToolWindow)
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            DrawVisualiser vis;
            Drawable target;

            Children = new[]
            {
                target = new TestSceneDynamicDepth
                {
                    Anchor = Anchor.BottomRight,
                    Origin = Anchor.BottomRight,
                    Size = new Vector2(0.5f)
                },
                vis = new DrawVisualiser(),
            };

            vis.Show();
            vis.Target = target;
        }
    }
}
