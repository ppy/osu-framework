// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneSliderBar : ManualInputManagerTestScene
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        public readonly BindableDouble SliderBarValue; //keep a reference to avoid GC of the bindable
        public readonly SpriteText SliderBarText;
        public readonly SliderBar<double> SliderBar;
        public readonly SliderBar<double> TransferOnCommitSliderBar;

        public TestSceneSliderBar()
        {
            SliderBarValue = new BindableDouble
            {
                MinValue = -10,
                MaxValue = 10
            };

            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(5),
                Spacing = new Vector2(5, 5),
                Children = new Drawable[]
                {
                    SliderBarText = new SpriteText
                    {
                        Text = $"Value of Bindable: {SliderBarValue.Value}",
                    },
                    new SpriteText
                    {
                        Text = "BasicSliderBar:",
                    },
                    SliderBar = new BasicSliderBar<double>
                    {
                        Size = new Vector2(200, 10),
                        BackgroundColour = Color4.White,
                        SelectionColour = Color4.Pink,
                        KeyboardStep = 1,
                        Current = SliderBarValue
                    },
                    new SpriteText
                    {
                        Text = "w/ RangePadding:",
                    },
                    new BasicSliderBar<double>
                    {
                        Size = new Vector2(200, 10),
                        RangePadding = 20,
                        BackgroundColour = Color4.White,
                        SelectionColour = Color4.Pink,
                        KeyboardStep = 1,
                        Current = SliderBarValue
                    },
                    new SpriteText
                    {
                        Text = "w/ TransferValueOnCommit:",
                    },
                    TransferOnCommitSliderBar = new BasicSliderBar<double>
                    {
                        TransferValueOnCommit = true,
                        Size = new Vector2(200, 10),
                        BackgroundColour = Color4.White,
                        SelectionColour = Color4.Pink,
                        KeyboardStep = 1,
                        Current = SliderBarValue
                    },
                }
            });
        }
    }
}
