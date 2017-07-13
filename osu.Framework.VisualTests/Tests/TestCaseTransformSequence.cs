// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTransformSequence : GridTestCase
    {
        public override string Description => @"Sequences (potentially looping) of transforms";

        private readonly Container[] boxes;

        public TestCaseTransformSequence() : base(2, 2)
        {
            string[] labels =
            {
                "Looped single transform with 3 iterations",
                "Looped single transform with 1 sec pause",
                "Looped double transforms",
                "Looped double transforms with 1 sec pause"
            };

            boxes = new Container[Rows * Cols];
            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = labels[i],
                        TextSize = 20,
                    },
                    boxes[i] = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.25f),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Radius = 20,
                            Colour = Color4.Blue,
                        },
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        }
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

            //boxes[2].RotateTo(360, 1000).ScaleTo(2, 500).Then().RotateTo(0, 500).ScaleTo(0, 1000);

            //boxes[2].RotateTo(360, 1000).Then().RotateTo(0, 1000).ScaleTo(2, 500).Then().RotateTo(360, 1000).FadeEdgeEffectTo(Color4.Red, 1000);

            boxes[2].RotateTo(360, 1000)
            .Then(
                b => b.RotateTo(0, 1000),
                b => b.ScaleTo(2, 500)
            )
            .Then(
                () => boxes[2].RotateTo(360, 1000),
                () => boxes[2].ScaleTo(0.5f, 1000)
            ).WaitForCompletion().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500);

            /*.Loop(1000, 2)
            .Then(
                () => boxes[2].RotateTo(360, 1000),
                () => boxes[2].ScaleTo(0, 1000)
            );*/


            using (boxes[3].BeginLoopedSequence(1000))
            {
                boxes[3].RotateTo(360, 1000);
                using (boxes[3].BeginDelayedSequence(1000))
                    boxes[3].RotateTo(0, 1000);
            }
        }
    }
}
