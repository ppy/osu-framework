// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Globalization;
using System.Threading;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSpinForever : TestCase
    {
        private readonly Container box;
        private double startTime;
        private readonly SpriteText textActual;
        private readonly SpriteText textRotationCount;
        private readonly SpriteText textExpected;
        private readonly SpriteText textDrift;
        private bool driftDetected;

        public TestCaseSpinForever()
        {
            Children = new Drawable[]
            {
                box = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.25f),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
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
                },
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new SpriteText { Text = "Rotation Count: " },
                        textRotationCount = new SpriteText(),
                        new SpriteText { Text = "Actual Angle: " },
                        textActual = new SpriteText(),
                        new SpriteText { Text = "Expected Angle: " },
                        textExpected = new SpriteText(),
                        new SpriteText { Text = "Drift: " },
                        textDrift = new SpriteText(),
                    }
                }
            };
        }

        private const double spin_duration = 1;

        private float expectedRotation => (float)((box.Time.Current - startTime) % spin_duration / spin_duration * 360);

        private int rotationCount => (int)((box.Time.Current - startTime) / spin_duration);

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            textRotationCount.Text = rotationCount.ToString();
            textActual.Text = box.Rotation.ToString(CultureInfo.InvariantCulture);
            textExpected.Text = expectedRotation.ToString(CultureInfo.InvariantCulture);
            textDrift.Text = (box.Rotation - expectedRotation).ToString(CultureInfo.InvariantCulture);

            driftDetected = !Precision.AlmostEquals(box.Rotation, expectedRotation);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            startTime = box.Time.Current;

            box.Spin(spin_duration, RotationDirection.Clockwise);

            AddUntilStep(() => rotationCount > 1000);
            AddAssert("ensure no dirft", () => !driftDetected);
            AddStep("delay execution", () => Thread.Sleep(500));
            AddWaitStep(5);
            AddAssert("ensure no dirft", () => !driftDetected);
        }
    }
}
