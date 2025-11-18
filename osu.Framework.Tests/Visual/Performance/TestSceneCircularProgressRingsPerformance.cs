// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneCircularProgressRingsPerformance : FrameworkTestScene
    {
        public TestSceneCircularProgressRingsPerformance()
        {
            for (int i = 0; i < 100; i++)
            {
                Add(new CircularProgress
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(10 * i),
                    InnerRadius = 0.1f / (i + 1),
                    Progress = 1
                });
            }
        }
    }
}
