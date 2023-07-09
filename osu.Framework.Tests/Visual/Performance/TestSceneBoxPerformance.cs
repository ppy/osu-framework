// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneBoxPerformance : PerformanceTestScene
    {
        protected readonly BindableFloat FillWidth = new BindableFloat();
        protected readonly BindableFloat FillHeight = new BindableFloat();
        protected readonly BindableInt SpritesCount = new BindableInt();
        protected readonly BindableBool GradientColour = new BindableBool();
        protected readonly BindableBool RandomiseColour = new BindableBool();

        public FillFlowContainer Flow { get; private set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Boxes");
            AddSliderStep("fill width", 0.01f, 1.0f, 0.1f, v => FillWidth.Value = v);
            AddSliderStep("fill height", 0.01f, 1.0f, 0.1f, v => FillHeight.Value = v);
            AddSliderStep("sprites count", 1, 1000, 100, v => SpritesCount.Value = v);
            AddToggleStep("gradient colour", v => GradientColour.Value = v);
            AddToggleStep("randomise colour", v => RandomiseColour.Value = v);

            Child = Flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding(20f),
                Spacing = new Vector2(20f),
            };

            SpritesCount.BindValueChanged(v =>
            {
                for (int i = v.OldValue - 1; i >= v.NewValue; i--)
                    Flow.Remove(Flow.Children[i], true);

                for (int i = v.OldValue; i < v.NewValue; i++)
                    Flow.Add(CreateDrawable());
            }, true);
        }

        protected void Recreate()
        {
            Flow.Clear();

            for (int i = 0; i < SpritesCount.Value; i++)
                Flow.Add(CreateDrawable());
        }

        protected virtual Drawable CreateDrawable() => new TestBox
        {
            FillWidth = { BindTarget = FillWidth },
            FillHeight = { BindTarget = FillHeight },
            GradientColour = { BindTarget = GradientColour },
            RandomiseColour = { BindTarget = RandomiseColour },
        };

        protected partial class TestBox : Sprite
        {
            public readonly IBindable<float> FillWidth = new BindableFloat();
            public readonly IBindable<float> FillHeight = new BindableFloat();
            public readonly IBindable<bool> GradientColour = new BindableBool();
            public readonly IBindable<bool> RandomiseColour = new BindableBool();

            [BackgroundDependencyLoader]
            private void load(IRenderer renderer)
            {
                RelativeSizeAxes = Axes.Both;
                Texture = renderer.WhitePixel;

                FillWidth.BindValueChanged(v => Width = v.NewValue, true);
                FillHeight.BindValueChanged(v => Height = v.NewValue, true);

                RandomiseColour.BindValueChanged(_ => updateColour());
                GradientColour.BindValueChanged(_ => updateColour(), true);

                void updateColour()
                {
                    if (GradientColour.Value)
                    {
                        Colour = new ColourInfo
                        {
                            TopLeft = RandomiseColour.Value ? getRandomColour() : Color4.Red,
                            TopRight = RandomiseColour.Value ? getRandomColour() : Color4.Blue,
                            BottomLeft = RandomiseColour.Value ? getRandomColour() : Color4.Green,
                            BottomRight = RandomiseColour.Value ? getRandomColour() : Color4.Yellow
                        };
                    }
                    else
                        Colour = RandomiseColour.Value ? getRandomColour() : Color4.White;
                }
            }

            private Colour4 getRandomColour() => new Colour4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1f);
        }
    }
}
