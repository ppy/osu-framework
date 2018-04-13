// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseTransformSequence : GridTestCase
    {
        private readonly Container[] boxes;

        public TestCaseTransformSequence()
            : base(3, 3)
        {
            boxes = new Container[Rows * Cols];
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            testFinish();
            testClear();
        }

        private void testFinish()
        {
            AddStep("Animate", delegate
            {
                setup();
                animate();
            });

            AddStep($"{nameof(FinishTransforms)}", delegate
            {
                foreach (var box in boxes)
                    box.FinishTransforms();
            });

            AddAssert("finalize triggered", () => finalizeTriggered);
        }

        private void testClear()
        {
            AddStep("Animate", delegate
            {
                setup();
                animate();
            });

            AddStep($"{nameof(ClearTransforms)}", delegate
            {
                foreach (var box in boxes)
                    box.ClearTransforms();
            });

            AddAssert("finalize triggered", () => finalizeTriggered);
        }

        private void setup()
        {
            finalizeTriggered = false;

            string[] labels =
            {
                "Spin after 2 seconds",
                "Loop(1 sec pause; 1 sec rotate)",
                "Complex transform 1 (should end in sync with CT2)",
                "Complex transform 2 (should end in sync with CT1)",
                $"Red on {nameof(TransformSequence<Container>)}.{nameof(TransformSequence<Container>.OnAbort)}",
                $"Red on {nameof(TransformSequence<Container>)}.{nameof(TransformSequence<Container>.Finally)}",
                "Red after instant transform",
                "Red after instant transform 1 sec in the past",
                "Red after 1 sec transform 1 sec in the past",
            };

            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).Children = new Drawable[]
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
                };
            }
        }

        private bool finalizeTriggered;

        private void animate()
        {
            boxes[0].Delay(500).Then(500).Then(500).Then(
                b => b.Delay(500).Spin(1000, RotationDirection.CounterClockwise)
            );

            boxes[1].Delay(1000).Loop(1000, 10, b => b.RotateTo(0).RotateTo(340, 1000));

            boxes[2].RotateTo(0).ScaleTo(1).RotateTo(360, 1000)
            .Then(1000,
                b => b.RotateTo(0, 1000),
                b => b.ScaleTo(2, 500)
            )
            .Then().RotateTo(360, 1000).ScaleTo(0.5f, 1000)
            .Then().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500);

            boxes[3].RotateTo(0).ScaleTo(1).RotateTo(360, 500)
            .Then(1000,
                b => b.RotateTo(0),
                b => b.ScaleTo(2)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0).RotateTo(360, 1000)).Delay(500).ScaleTo(0.5f, 500)
            )
            .Then().FadeEdgeEffectTo(Color4.Red, 1000).ScaleTo(2, 500)
            .Finally(_ => finalizeTriggered = true);


            boxes[4].RotateTo(0).ScaleTo(1).RotateTo(360, 500)
            .Then(1000,
                b => b.RotateTo(0),
                b => b.ScaleTo(2)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0).RotateTo(360, 1000)),
                b => b.ScaleTo(0.5f, 500)
            )
            .OnAbort(b => b.FadeEdgeEffectTo(Color4.Red, 1000));


            boxes[5].RotateTo(0).ScaleTo(1).RotateTo(360, 500)
            .Then(1000,
                b => b.RotateTo(0),
                b => b.ScaleTo(2)
            )
            .Then(
                b => b.Loop(500, 2, d => d.RotateTo(0).RotateTo(360, 1000)),
                b => b.ScaleTo(0.5f, 500)
            )
            .Finally(b => b.FadeEdgeEffectTo(Color4.Red, 1000));

            boxes[6].RotateTo(200)
            .Finally(b => b.FadeEdgeEffectTo(Color4.Red, 1000));

            boxes[7].Delay(-1000).RotateTo(200)
            .Finally(b => b.FadeEdgeEffectTo(Color4.Red, 1000));

            boxes[8].Delay(-1000).RotateTo(200, 1000)
            .Finally(b => b.FadeEdgeEffectTo(Color4.Red, 1000));
        }
    }
}
