// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Performance;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneFillRate : PerformanceTestScene
    {
        private float fillWidth;
        private float fillHeight;
        private int spritesCount;
        private bool gradientColour;
        private bool randomiseColour;

        public FillFlowContainer Flow { get; private set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Child = Flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(20f),
                Spacing = new Vector2(20f),
            };

            AddLabel("Boxes");
            AddSliderStep("fill width", 0.01f, 1.0f, 0.1f, v => fillWidth = v);
            AddSliderStep("fill height", 0.01f, 1.0f, 0.1f, v => fillHeight = v);
            AddSliderStep("sprites count", 1, 1000, 100, v => spritesCount = v);
            AddToggleStep("gradient colour", v => gradientColour = v);
            AddToggleStep("randomise colour", v => randomiseColour = v);
            AddStep("recreate", recreate);
        }

        private void recreate()
        {
            Flow.Clear();

            for (int i = 0; i < spritesCount; i++)
                Flow.Add(CreateDrawable());
        }

        protected virtual Drawable CreateDrawable()
        {
            var sprite = new Box();

            if (gradientColour)
            {
                sprite.Colour = new ColourInfo
                {
                    TopLeft = randomiseColour ? getRandomColour() : Color4.Red,
                    TopRight = randomiseColour ? getRandomColour() : Color4.Blue,
                    BottomLeft = randomiseColour ? getRandomColour() : Color4.Green,
                    BottomRight = randomiseColour ? getRandomColour() : Color4.Yellow
                };
            }
            else if (randomiseColour)
                sprite.Colour = getRandomColour();

            sprite.RelativeSizeAxes = Axes.Both;
            sprite.Size = new Vector2(fillWidth, fillHeight);
            return sprite;
        }

        private Colour4 getRandomColour() => new Colour4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1f);
    }
}
