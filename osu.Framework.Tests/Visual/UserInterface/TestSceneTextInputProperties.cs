// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneTextInputProperties : FrameworkTestScene
    {
        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Create text boxes", () =>
            {
                FillFlowContainer flow;
                Child = new BasicScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = flow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Width = 0.9f,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Spacing = new Vector2(20, 13),
                    }
                };

                foreach (var textInputType in Enum.GetValues<TextInputType>())
                {
                    flow.Add(new BasicTextBox
                    {
                        TabbableContentContainer = flow,
                        RelativeSizeAxes = Axes.X,
                        Height = 40,
                        Width = 0.45f,
                        PlaceholderText = $"{textInputType} (allow IME)",
                        InputProperties = new TextInputProperties
                        {
                            Type = textInputType,
                            AllowIme = true
                        },
                    });
                    flow.Add(new BasicTextBox
                    {
                        TabbableContentContainer = flow,
                        RelativeSizeAxes = Axes.X,
                        Height = 40,
                        Width = 0.45f,
                        PlaceholderText = $"{textInputType} (no IME)",
                        InputProperties = new TextInputProperties
                        {
                            Type = textInputType,
                            AllowIme = false
                        },
                    });
                }
            });
        }
    }
}
