// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneFileSelector : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new BasicFileSelector { RelativeSizeAxes = Axes.Both });
        }
    }
}
