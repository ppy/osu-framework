// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTransformSequence : GridTestCase
    {
        public override string Description => @"Sequences (potentially looping) of transforms";

        private readonly Drawable[] boxes;

        public TestCaseTransformSequence() : base(2, 2)
        {
            string[] labels =
            {
                "Looped single transform with 3 iterations",
                "Looped single transform with 1 sec pause",
                "Looped double transforms",
                "Looped double transforms with 1 sec pause"
            };

            boxes = new Drawable[Rows * Cols];
            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).Add(new[]
                {
                    new SpriteText
                    {
                        Text = labels[i],
                        TextSize = 20,
                    },
                    boxes[i] = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.25f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                });
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            using (boxes[0].BeginLoopedSequence(0, 3))
                boxes[0].RotateTo(360, 1000);

            using (boxes[1].BeginLoopedSequence(1000))
                boxes[1].RotateTo(360, 1000);

            using (boxes[2].BeginLoopedSequence())
            {
                boxes[2].RotateTo(360, 1000);
                using (boxes[2].BeginDelayedSequence(1000))
                    boxes[2].RotateTo(0, 1000);
            }

            using (boxes[3].BeginLoopedSequence(1000))
            {
                boxes[3].RotateTo(360, 1000);
                using (boxes[3].BeginDelayedSequence(1000))
                    boxes[3].RotateTo(0, 1000);
            }
        }
    }
}
