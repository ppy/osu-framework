// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTransformSequence : GridTestCase
    {
        public override string Description => @"Sequences (potentially looping) of transforms";

        private readonly Container[] boxes;

        public TestCaseTransformSequence() : base(3, 2)
        {
            string[] labels =
            {
                "Spin after 2 seconds",
                "Loop(1 sec pause; 1 sec rotate)",
                "Complex transform 1 (should end in sync with CT2)",
                "Complex transform 2 (should end in sync with CT1)",
                "Red when abort",
                "Red when finally",
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

            AddStep("Abort all", delegate
            {
                foreach (var box in boxes)
                    box.ClearTransforms();
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            boxes[0].Delayed(500).Then(500).Then(500).Then(
                b => b.Delayed(500).Spin(1000)
            );

            boxes[1].Delayed(1000).Loop(1000, b => b.RotateTo(0).RotateTo(360, 1000));

            boxes[2].RotateTo(360, 1000)
            .Then(1000,
                b => b.RotateTo(0, 1000),
                b => b.ScaleTo(2, 500)
            )
            .Then().RotateTo(360, 1000).ScaleTo(0.5f, 1000)
            .Then().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500);

            boxes[3].RotateTo(360, 500)
            .Then(1000,
                b => b.RotateTo(0),
                b => b.ScaleTo(2)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0).RotateTo(360, 1000)),
                b => b.ScaleTo(0.5f, 500)
            )
            .Then().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500);


            boxes[4].RotateTo(360, 500)
            .Then(1000,
                b => b.RotateTo(0),
                b => b.ScaleTo(2)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0).RotateTo(360, 1000)),
                b => b.ScaleTo(0.5f, 500)
            )
            .Catch(() =>
            {
                boxes[4].FadeEdgeEffectTo(Color4.Red, 1000);
            });


            boxes[5].RotateTo(360, 500)
            .Then(1000,
                b => b.RotateTo(0),
                b => b.ScaleTo(2)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0).RotateTo(360, 1000)),
                b => b.ScaleTo(0.5f, 500)
            )
            .Finally(() =>
            {
                boxes[5].FadeEdgeEffectTo(Color4.Red, 1000);
            });
        }
    }
}
