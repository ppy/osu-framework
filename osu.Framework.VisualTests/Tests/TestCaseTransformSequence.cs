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

            // Loop a rotation from 0 to 360 degrees with duration 1000 ms
            boxes[0].Loop(b => b.RotateTo(0, 0).RotateTo(360, 1000));

            // After 1000 ms, loop a rotation from 0 to 360 degrees with duration 1000 ms, but pause for 1000 ms between rotations.
            boxes[1].Delayed(1000).Loop(1000, b => b.RotateTo(0, 0).RotateTo(360, 1000));

            // Rotate by 360 degrees during 1000 ms, then simultaneously rotate back during 1000 ms and 
            // scale to 2 during 500 ms. Then, rotate by 360 degrees during 1000 ms again, and simultaneously
            // scale to 0.5 during 1000 ms.
            // Lastly, simultaneously fade the edge effect to red during 1000 ms and scale to 2 during 500 ms.
            boxes[2].RotateTo(360, 1000)
            .Then(
                b => b.RotateTo(0, 1000),
                b => b.ScaleTo(2, 500)
            )
            .WaitForCompletion().RotateTo(360, 1000).ScaleTo(0.5f, 1000)
            .WaitForCompletion().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500);

            // Rotate by 360 during 500 ms degrees, then instantly rotate back and scale to 2,
            // then loop a 1-second rotation twice with 500 ms break between them, and a simultaneous scaling operation for 500 ms.
            // Lastly, simultaneously fade the edge effect to red during 1000 ms and scale to 2 during 500 ms.
            boxes[3].RotateTo(360, 500)
            .Then(
                b => b.RotateTo(0, 0),
                b => b.ScaleTo(2, 0)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0, 0).RotateTo(360, 1000)),
                b => b.ScaleTo(0.5f, 500)
            )
            .WaitForCompletion().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500);
        }
    }
}
