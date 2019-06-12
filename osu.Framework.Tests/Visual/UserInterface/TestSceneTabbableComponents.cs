// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using System.Collections.Generic;
using NUnit.Framework;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTabbableComponents : ManualInputManagerTestScene
    {
        private const float height = 350;
        private const float width = 450;
        private const float spacing = 20;
        private const float component_height = 30;
        private const float component_width = (width - spacing - (spacing * 2)) / 2;

        private readonly BasicDropdown<string> dropdown;
        private readonly BasicTextBox text1;
        private readonly BasicTextBox text2;
        private readonly BasicTextBox textLast;

        public TestSceneTabbableComponents()
        {
            Children = new Drawable[]
            {
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = width,
                    Height = height,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = FrameworkColour.GreenDarker,
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding(spacing),
                            Children = new Drawable[]
                            {
                                dropdown = new BasicDropdown<string>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Depth = -1,
                                    Items = new List<string> { "one", "two", "three" },
                                    TabbableContentContainer = this
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Height = height
                                             - (spacing * 2)
                                             - (spacing * 2)
                                             - component_height,
                                    Y = spacing + component_height,
                                    Children = new Drawable[]
                                    {
                                        new ScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = height - (spacing * 4) - (component_height * 2),
                                            Children = new Drawable[]
                                            {
                                                new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Spacing = new Vector2(spacing),
                                                    Children = new Drawable[]
                                                    {
                                                        text1 = new BasicTextBox
                                                        {
                                                            Width = component_width,
                                                            Height = component_height,
                                                            TabbableContentContainer = this,
                                                        },
                                                        text2 = new BasicTextBox
                                                        {
                                                            Width = component_width,
                                                            Height = component_height,
                                                            TabbableContentContainer = this,
                                                        },
                                                        new BasicTextBox
                                                        {
                                                            Width = component_width,
                                                            Height = component_height,
                                                            TabbableContentContainer = this,
                                                        },
                                                        new BasicCheckbox
                                                        {
                                                            Scale = new Vector2(component_width / 30f, 1),
                                                        },
                                                        new BasicCheckbox
                                                        {
                                                            Scale = new Vector2(component_width / 30f, 1),
                                                        },
                                                        new BasicTextBox
                                                        {
                                                            Width = component_width,
                                                            Height = component_height,
                                                            TabbableContentContainer = this,
                                                        },
                                                        textLast = new BasicTextBox
                                                        {
                                                            Width = component_width,
                                                            Height = component_height,
                                                            TabbableContentContainer = this,
                                                        },
                                                        new BasicSliderBar<int>
                                                        {
                                                            Width = component_width,
                                                            Height = component_height,
                                                            Current = new Framework.Bindables.BindableInt
                                                            {
                                                                MinValue = -5,
                                                                MaxValue = 5,
                                                            }
                                                        }
                                                    },
                                                }
                                            }
                                        },
                                    }
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Spacing = new Vector2(spacing),
                                    Children = new Drawable[]
                                    {
                                        new Button
                                        {
                                            Width = component_width,
                                            Height = component_height,
                                            Text = "Button1",
                                            BackgroundColour = FrameworkColour.Green,
                                        },
                                        new Button
                                        {
                                            Width = component_width,
                                            Height = component_height,
                                            Text = "Button2",
                                            BackgroundColour = FrameworkColour.Green,
                                        },
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Clear focus", () =>
            {
                InputManager.MoveMouseTo(new Vector2(0));
                InputManager.Click(MouseButton.Left);
            });
        }

        [Test]
        public void TestTabWithNoFocus()
        {
            AddStep("Press tab with no focus", () =>
            {
                InputManager.PressKey(Key.Tab);
                InputManager.ReleaseKey(Key.Tab);
            });
            AddAssert("No focused item", () => InputManager.FocusedDrawable == null);
        }

        [Test]
        public void TestTabAwayFromDropdown()
        {
            performTabToTest(dropdown, text1);
        }

        [Test]
        public void TestTabToDropdown()
        {
            performTabToTest(textLast, dropdown);
        }

        [Test]
        public void TestTabFromTexttoText()
        {
            performTabToTest(text1, text2);
        }

        private void performTabToTest(Drawable source, Drawable target)
        {
            AddStep($"Click {source}", () =>
            {
                InputManager.MoveMouseTo(source);
                InputManager.Click(MouseButton.Left);
            });
            AddStep($"Press tab with {source} focused", () =>
            {
                InputManager.PressKey(Key.Tab);
                InputManager.ReleaseKey(Key.Tab);
            });

            // HandleNonPositionInput is overridden by each tabbable container to only accept input if it was the last tabbed to item.
            // In the case of dropdowns, this is any one of its children.
            AddAssert($"{target} focused", () => target.HandleNonPositionalInput);
        }
    }
}
