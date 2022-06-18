// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    [Ignore("This test needs a game host to be useful.")]
    public class TestSceneStressGLDisposal : FrameworkTestScene
    {
        private FillFlowContainer fillFlow;

        protected override double TimePerAction => 0.001;

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Add(fillFlow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Full
            });
        });

        [Test]
        public void TestAddRemoveDrawable()
        {
            AddRepeatStep("add/remove drawables", () =>
            {
                for (int i = 0; i < 50; i++)
                {
                    fillFlow.Add(new Box { Size = new Vector2(5) });

                    if (fillFlow.Count > 100)
                    {
                        fillFlow.Remove(fillFlow.First());
                    }
                }
            }, 100000);
        }
    }
}
