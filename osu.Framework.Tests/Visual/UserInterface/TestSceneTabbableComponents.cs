// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTabbableComponents : ManualInputManagerTestScene
    {
        private const float component_width = 300f;
        private const float component_height = 35f;
        private BasicTextBox text1;
        private BasicTextBox text2;
        private BasicTextBox text4;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new TabbableContentContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding(20),
                    Spacing = new Vector2(20),
                    Children = new[]
                    {
                        text1 = new BasicTextBox
                        {
                            Width = component_width,
                            Height = component_height,
                            Text = "I'm a textbox!",
                        },
                        text2 = new BasicTextBox
                        {
                            Width = component_width,
                            Height = component_height,
                            Text = "I'm a normal textbox too!",
                        },
                        new BasicTextBox
                        {
                            Width = component_width,
                            Height = component_height,
                            ReadOnly = true,
                            Text = "I'm a read-only textbox.",
                            Colour = Color4.Tomato
                        },
                        text4 = new BasicTextBox
                        {
                            Width = component_width,
                            Height = component_height,
                            Text = "Just another normal textbox",
                        }
                    }
                }
            });
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
        public void TestTabFromTexttoText()
        {
            performTabToTest(text1, text2);
        }

        [Test]
        public void TestTabFromTexttoDisabledText()
        {
            // The spot where text3 would be is read-only, so it should have been skipped.
            performTabToTest(text2, text4);
        }

        [Test]
        public void TestWrappingTabTest()
        {
            performTabToTest(text4, text1);
        }

        [Test]
        public void TestTabInReverse()
        {
            performTabToTest(text2, text1, true);
        }

        private void performTabToTest(Drawable source, Drawable expectedTarget, bool reversed = false)
        {
            AddStep($"Click {source}", () =>
            {
                InputManager.MoveMouseTo(source);
                InputManager.Click(MouseButton.Left);
            });
            AddStep($"Press tab with {source} focused", () =>
            {
                if (reversed)
                    InputManager.PressKey(Key.ShiftLeft);

                InputManager.PressKey(Key.Tab);
                InputManager.ReleaseKey(Key.Tab);

                if (reversed)
                    InputManager.ReleaseKey(Key.ShiftLeft);
            });

            AddAssert($"{expectedTarget} focused", () => InputManager.FocusedDrawable == expectedTarget);
        }
    }
}
