// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTooltip : ManualInputManagerTestScene
    {
        private TestTooltipContainer tooltipContainer;

        private TooltipSpriteText tooltipText;
        private TooltipSpriteText instantTooltipText;
        private CustomTooltipSpriteText customTooltipTextA;
        private CustomTooltipSpriteText customTooltipTextB;
        private CustomTooltipSpriteTextAlt customTooltipTextAlt;
        private TooltipSpriteText emptyTooltipText;
        private TooltipSpriteText nullTooltipText;
        private TooltipTextBox tooltipTextBox;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddToggleStep("add tooltips (cursor/cursor-less)", generateTest);
        }

        [Test]
        public void TestTooltip()
        {
            ITooltip originalInstance = null;

            hoverTooltipProvider(() => tooltipText);

            AddStep("get tooltip instance", () => originalInstance = tooltipContainer.CurrentTooltip);
            assertTooltipText(() => tooltipText.TooltipText);

            hoverTooltipProvider(() => tooltipText);

            AddAssert("tooltip reused", () => tooltipContainer.CurrentTooltip == originalInstance);
            assertTooltipText(() => tooltipText.TooltipText);
        }

        [Test]
        public void TestInstantTooltip()
        {
            hoverTooltipProvider(() => instantTooltipText, false);
            assertTooltipText(() => instantTooltipText.TooltipText);
        }

        [Test]
        public void TestCustomTooltip()
        {
            ITooltip originalInstance = null;

            hoverTooltipProvider(() => customTooltipTextA);

            AddStep("get tooltip instance", () => originalInstance = tooltipContainer.CurrentTooltip);
            AddAssert("custom tooltip used", () => originalInstance.GetType() == typeof(CustomTooltip));
            assertTooltipText(() => ((CustomContent)customTooltipTextA.TooltipContent).Text);

            hoverTooltipProvider(() => customTooltipTextB);

            AddAssert("custom tooltip reused", () => tooltipContainer.CurrentTooltip == originalInstance);
            assertTooltipText(() => ((CustomContent)customTooltipTextB.TooltipContent).Text);
        }

        [Test]
        public void TestDifferentCustomTooltips()
        {
            hoverTooltipProvider(() => customTooltipTextA);
            assertTooltipText(() => ((CustomContent)customTooltipTextA.TooltipContent).Text);

            AddAssert("current tooltip type normal", () => tooltipContainer.CurrentTooltip.GetType() == typeof(CustomTooltip));

            hoverTooltipProvider(() => customTooltipTextAlt);
            assertTooltipText(() => ((CustomContent)customTooltipTextAlt.TooltipContent).Text);

            AddAssert("current tooltip type alt", () => tooltipContainer.CurrentTooltip.GetType() == typeof(CustomTooltipAlt));

            hoverTooltipProvider(() => customTooltipTextB);
            assertTooltipText(() => ((CustomContent)customTooltipTextB.TooltipContent).Text);

            AddAssert("current tooltip type normal", () => tooltipContainer.CurrentTooltip.GetType() == typeof(CustomTooltip));
        }

        [Test]
        public void TestEmptyTooltip()
        {
            AddStep("hover empty tooltip", () => InputManager.MoveMouseTo(emptyTooltipText));
            AddAssert("tooltip not shown", () => tooltipContainer.CurrentTooltip?.IsPresent != true);
        }

        [Test]
        public void TestNullTooltip()
        {
            AddStep("hover null tooltip", () => InputManager.MoveMouseTo(nullTooltipText));
            AddAssert("tooltip not shown", () => tooltipContainer.CurrentTooltip?.IsPresent != true);
        }

        [Test]
        public void TestUpdatingTooltip()
        {
            hoverTooltipProvider(() => tooltipTextBox);
            assertTooltipText(() => tooltipTextBox.Text);

            AddStep("update text", () => tooltipTextBox.Text = "updated!");

            assertTooltipText(() => "updated!");
        }

        private void hoverTooltipProvider(Func<Drawable> getProvider, bool waitForDisplay = true)
        {
            AddStep("hover away from tooltips", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddAssert("tooltip hidden", () => tooltipContainer.CurrentTooltip?.IsPresent != true);

            AddStep("hover tooltip", () => InputManager.MoveMouseTo(getProvider()));

            if (waitForDisplay)
                AddUntilStep("wait for tooltip", () => tooltipContainer.CurrentTooltip?.IsPresent == true);
            else
                AddAssert("tooltip instantly displayed", () => tooltipContainer.CurrentTooltip?.IsPresent == true);
        }

        private void assertTooltipText(Func<LocalisableString> expected)
        {
            AddAssert("tooltip text matching", () =>
            {
                var drawableTooltip = (Drawable)tooltipContainer.CurrentTooltip;
                return drawableTooltip.ChildrenOfType<IHasText>().Count(t => t.Text == expected()) == 1;
            });
        }

        private TooltipBox makeBox(Anchor anchor) => new TooltipBox
        {
            RelativeSizeAxes = Axes.Both,
            Size = new Vector2(0.2f),
            Anchor = anchor,
            Origin = anchor,
            Colour = Color4.Blue,
            TooltipText = $"{anchor}",
        };

        private void generateTest(bool cursorlessTooltip)
        {
            Clear();

            CursorContainer cursor = null;

            if (!cursorlessTooltip)
            {
                cursor = new RectangleCursorContainer();
                Add(cursor);
            }

            Add(tooltipContainer = new TestTooltipContainer(cursor)
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new TooltipBox
                            {
                                TooltipText = "Outer Tooltip",
                                Colour = Color4.CornflowerBlue,
                                Size = new Vector2(300, 300),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre
                            },
                            new TooltipBox
                            {
                                TooltipText = "Inner Tooltip",
                                Size = new Vector2(150, 150),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            tooltipText = new TooltipSpriteText("this text has a tooltip!"),
                            instantTooltipText = new InstantTooltipSpriteText("this text has an instant tooltip!"),
                            customTooltipTextA = new CustomTooltipSpriteText("this one is custom!"),
                            customTooltipTextB = new CustomTooltipSpriteText("this one is also!"),
                            customTooltipTextAlt = new CustomTooltipSpriteTextAlt("but this one is different."),
                            emptyTooltipText = new InstantTooltipSpriteText("this text has an empty tooltip!", string.Empty),
                            nullTooltipText = new InstantTooltipSpriteText("this text has a nulled tooltip!", null),
                            tooltipTextBox = new TooltipTextBox
                            {
                                Text = "with real time updates!",
                                Size = new Vector2(400, 30),
                            },
                            new TooltipContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Child = new TooltipSpriteText("Nested tooltip; uses no cursor in all cases!"),
                            },
                            new TooltipTooltipContainer("This tooltip container has a tooltip itself!")
                            {
                                AutoSizeAxes = Axes.Both,
                                Child = new Container
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Child = new TooltipSpriteText("Nested tooltip; uses no cursor in all cases; parent TooltipContainer has a tooltip"),
                                }
                            },
                            new Container
                            {
                                Child = new FillFlowContainer
                                {
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 8),
                                    Children = new[]
                                    {
                                        new Container
                                        {
                                            Child = new Container
                                            {
                                                Child = new TooltipSpriteText("Tooltip within containers with zero size; i.e. parent is never hovered."),
                                            }
                                        },
                                        new Container
                                        {
                                            Child = new TooltipSpriteText("Other tooltip within containers with zero size; different nesting; overlap."),
                                        }
                                    }
                                }
                            }
                        },
                    }
                }
            });

            tooltipContainer.Add(makeBox(Anchor.BottomLeft));
            tooltipContainer.Add(makeBox(Anchor.TopRight));
            tooltipContainer.Add(makeBox(Anchor.BottomRight));
        }

        private class TestTooltipContainer : TooltipContainer
        {
            public new ITooltip CurrentTooltip => base.CurrentTooltip;

            public TestTooltipContainer(CursorContainer cursor)
                : base(cursor)
            {
            }
        }

        private class CustomTooltipSpriteText : Container, IHasCustomTooltip
        {
            public object TooltipContent { get; }

            public CustomTooltipSpriteText(string displayedContent, string tooltipContent = null)
            {
                TooltipContent = new CustomContent(tooltipContent ?? displayedContent);

                AutoSizeAxes = Axes.Both;
                Children = new[]
                {
                    new SpriteText
                    {
                        Text = displayedContent,
                    }
                };
            }

            public virtual ITooltip GetCustomTooltip() => new CustomTooltip();
        }

        private class CustomTooltipSpriteTextAlt : CustomTooltipSpriteText
        {
            public CustomTooltipSpriteTextAlt(string displayedContent, string tooltipContent = null)
                : base(displayedContent, tooltipContent)
            {
            }

            public override ITooltip GetCustomTooltip() => new CustomTooltipAlt();
        }

        private class CustomContent
        {
            public readonly LocalisableString Text;

            public CustomContent(string text)
            {
                Text = text;
            }
        }

        private class CustomTooltip : CompositeDrawable, ITooltip<CustomContent>
        {
            private static int i;

            private readonly SpriteText text;

            public CustomTooltip()
            {
                AutoSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDark,
                    },
                    text = new SpriteText
                    {
                        Font = FrameworkFont.Regular.With(size: 16),
                        Padding = new MarginPadding(5),
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Font = FontUsage.Default.With(size: 12),
                        Colour = Color4.Yellow,
                        Text = $"Custom tooltip instance {i++}"
                    },
                };
            }

            public void SetContent(CustomContent content) => text.Text = content.Text;

            public void Move(Vector2 pos) => Position = pos;
        }

        private class CustomTooltipAlt : CustomTooltip
        {
            public CustomTooltipAlt()
            {
                AutoSizeAxes = Axes.Both;

                Colour = Color4.Red;
            }
        }

        private class TooltipSpriteText : Container, IHasTooltip
        {
            public LocalisableString TooltipText { get; }

            public TooltipSpriteText(string displayedContent)
                : this(displayedContent, displayedContent)
            {
            }

            protected TooltipSpriteText(string displayedContent, string tooltipContent)
            {
                TooltipText = tooltipContent;

                AutoSizeAxes = Axes.Both;
                Children = new[]
                {
                    new SpriteText
                    {
                        Text = displayedContent,
                    }
                };
            }
        }

        private class InstantTooltipSpriteText : TooltipSpriteText, IHasAppearDelay
        {
            public InstantTooltipSpriteText(string tooltipContent)
                : base(tooltipContent, tooltipContent)
            {
            }

            public InstantTooltipSpriteText(string displayedContent, string tooltipContent)
                : base(displayedContent, tooltipContent)
            {
            }

            public double AppearDelay => 0;
        }

        private class TooltipTooltipContainer : TooltipContainer, IHasTooltip
        {
            public LocalisableString TooltipText { get; set; }

            public TooltipTooltipContainer(string tooltipText)
            {
                TooltipText = tooltipText;
            }
        }

        private class TooltipTextBox : BasicTextBox, IHasTooltip
        {
            public LocalisableString TooltipText => Text;
        }

        private class TooltipBox : Box, IHasTooltip
        {
            public LocalisableString TooltipText { get; set; }
        }

        private class RectangleCursorContainer : CursorContainer
        {
            protected override Drawable CreateCursor() => new RectangleCursor();

            private class RectangleCursor : Box
            {
                public RectangleCursor()
                {
                    Size = new Vector2(20, 40);
                }
            }
        }
    }
}
