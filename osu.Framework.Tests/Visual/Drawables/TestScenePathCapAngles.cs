// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osuTK.Graphics;
using osu.Framework.Graphics.Lines;
using osuTK;
using System;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestScenePathCapAngles : TestScene
    {
        private const int max_inner_steps = 9000;
        private const int max_outer_steps = 3000;
        private const float segment_length = 150f;

        private readonly Path path = new Path { PathRadius = 12 };
        private readonly SpriteText innerText = createLabel();
        private readonly SpriteText outerText = createLabel();

        private readonly Vector2 center = new Vector2(500f, 350f);

        private int innerStep;
        private int outerStep;

        public TestScenePathCapAngles()
        {
            AddRange(new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Children = new[]
                    {
                        innerText,
                        outerText,
                    },
                },
                path,
            });
        }

        protected override void Update()
        {
            base.Update();

            float innerAngle = MathHelper.TwoPi * innerStep / max_inner_steps;
            float outerAngle = MathHelper.TwoPi * outerStep / max_outer_steps;

            innerText.Text = "Inner angle: " + MathHelper.RadiansToDegrees(innerAngle).ToString("000.000");
            outerText.Text = "Outer angle: " + MathHelper.RadiansToDegrees(outerAngle).ToString("000.000");

            Vector2 inner = center + segment_length * new Vector2(MathF.Cos(innerAngle), MathF.Sin(innerAngle));
            Vector2 outer = inner + segment_length * new Vector2(MathF.Cos(outerAngle), MathF.Sin(outerAngle));

            path.Vertices = new[] { center, inner, outer, };

            innerStep = (innerStep + 1) % max_inner_steps;
            outerStep = (outerStep + 1) % max_outer_steps;
        }

        private static SpriteText createLabel() => new SpriteText
        {
            Font = new FontUsage(size: 20),
            Colour = Color4.White,
        };
    }
}
