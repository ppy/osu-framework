// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneBorderSmoothing : FrameworkTestScene
    {
        public TestSceneBorderSmoothing()
        {
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Direction = FillDirection.Full,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        new IssueButton
                        {
                            Text = "no fill"
                        },
                        new IssueButton
                        {
                            OverlayColour = Colour4.White.Opacity(0.0001f),
                            Text = "very transparent fill"
                        },
                        new IssueButton
                        {
                            OverlayColour = Colour4.Gray,
                            Text = "gray bg"
                        },
                        new IssueButton
                        {
                            OverlayColour = Colour4.White.Opacity(0.5f),
                            Text = "0.5 white bg"
                        },
                        new IssueButton
                        {
                            OverlayColour = Colour4.White,
                            Text = "white bg"
                        },
                        new IssueButton(false)
                        {
                            BackgroundColour = Colour4.Gray,
                            Text = "gray should match 1",
                        },
                        new IssueButton(false)
                        {
                            BackgroundColour = Colour4.White,
                            Text = "gray should match 2",
                            Alpha = 0.5f,
                        },
                        new IssueButton(borderColour: Colour4.Gray)
                        {
                            OverlayColour = Colour4.Gray,
                            Text = "gray to gray bg"
                        },
                        new IssueButton(borderColour: Colour4.Gray)
                        {
                            OverlayColour = Colour4.White.Opacity(0.5f),
                            Text = "gray to transparent white bg"
                        },
                    }
                }
            };

            AddSliderStep("adjust alpha", 0f, 1f, 1, val => Child.Alpha = val);
        }

        private class IssueButton : BasicButton
        {
            public Colour4? OverlayColour;

            public IssueButton(bool drawBorder = true, Colour4? borderColour = null)
            {
                AutoSizeAxes = Axes.None;
                Size = new Vector2(200);

                BackgroundColour = Colour4.Black;

                if (drawBorder)
                {
                    Content.Masking = true;
                    Content.MaskingSmoothness = 20;
                    Content.BorderThickness = 40;

                    Content.BorderColour = borderColour ?? Colour4.Red;
                }
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Add(new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = new Box
                    {
                        Alpha = OverlayColour.HasValue ? 1 : 0,
                        RelativeSizeAxes = Axes.Both,
                        Colour = OverlayColour ?? Colour4.Transparent,
                    }
                });
            }
        }
    }
}
