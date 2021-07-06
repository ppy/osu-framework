// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestScenePopoverContainer : ManualInputManagerTestScene
    {
        private Container[,] cells;
        private Container popoverWrapper;
        private PopoverContainer popoverContainer;
        private GridContainer gridContainer;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create popover container", () =>
            {
                Child = popoverWrapper = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    BorderThickness = 5,
                    BorderColour = Colour4.White,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            AlwaysPresent = true,
                            Colour = Colour4.Transparent
                        },
                        popoverContainer = new PopoverContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding(5),
                            Child = gridContainer = new GridContainer
                            {
                                RelativeSizeAxes = Axes.Both
                            }
                        }
                    }
                };

                cells = new Container[3, 3];

                for (int r = 0; r < 3; r++)
                {
                    for (int c = 0; c < 3; c++)
                        cells[r, c] = new Container { RelativeSizeAxes = Axes.Both };
                }

                gridContainer.Content = cells.ToJagged();
            });
        }

        [Test]
        public void TestSimpleText()
        {
            createContent(button => new BasicPopover
            {
                Child = new SpriteText
                {
                    Text = $"{button.Anchor} popover"
                }
            });

            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().First());
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover created", () => this.ChildrenOfType<Popover>().Any());

            AddStep("click popover", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<Popover>().Single().Body);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover still visible", () => this.ChildrenOfType<Popover>().Single().State.Value == Visibility.Visible);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().Last());
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover removed", () => !this.ChildrenOfType<Popover>().Any());
        }

        [Test]
        public void TestDragAwayDoesntHide()
        {
            createContent(button => new BasicPopover
            {
                Child = new SpriteText
                {
                    Text = $"{button.Anchor} popover"
                }
            });

            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().First());
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("popover created", () => this.ChildrenOfType<Popover>().Any());

            AddStep("mousedown popover", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<Popover>().Single().Body);
                InputManager.PressButton(MouseButton.Left);
            });
            AddAssert("popover still visible", () => this.ChildrenOfType<Popover>().Single().State.Value == Visibility.Visible);

            AddStep("move away", () => InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().Last()));

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("popover remains", () => this.ChildrenOfType<Popover>().Any());
        }

        [Test]
        public void TestInteractiveContent() => createContent(button =>
        {
            TextBox textBox;

            return new BasicPopover
            {
                Child = new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Width = 200,
                    AutoSizeAxes = Axes.Y,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        textBox = new BasicTextBox
                        {
                            PlaceholderText = $"{button.Anchor} text box",
                            Height = 30,
                            RelativeSizeAxes = Axes.X
                        },
                        new BasicButton
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 30,
                            Text = "Clear",
                            Action = () => textBox.Text = string.Empty
                        }
                    }
                }
            };
        });

        [Test]
        public void TestAutomaticLayouting()
        {
            DrawableWithPopover target = null;

            AddStep("add button", () => popoverContainer.Child = target = new DrawableWithPopover
            {
                Width = 200,
                Height = 30,
                RelativePositionAxes = Axes.Both,
                Text = "open",
                CreateContent = _ => new BasicPopover
                {
                    Child = new SpriteText
                    {
                        Text = "This popover follows its associated UI component",
                        Size = new Vector2(400)
                    }
                }
            });

            AddSliderStep("move X", 0f, 1, 0, x =>
            {
                if (target != null)
                    target.X = x;
            });

            AddSliderStep("move Y", 0f, 1, 0, y =>
            {
                if (target != null)
                    target.Y = y;
            });

            AddSliderStep("container width", 0f, 1, 1, width =>
            {
                if (popoverWrapper != null)
                    popoverWrapper.Width = width;
            });

            AddSliderStep("container height", 0f, 1, 1, height =>
            {
                if (popoverWrapper != null)
                    popoverWrapper.Height = height;
            });
        }

        private void createContent(Func<DrawableWithPopover, Popover> creationFunc)
            => AddStep("create content", () =>
            {
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        Anchor popoverAnchor = 0;
                        popoverAnchor |= (Anchor)((int)Anchor.x0 << i);
                        popoverAnchor |= (Anchor)((int)Anchor.y0 << j);

                        cells[j, i].Child = new DrawableWithPopover
                        {
                            Width = 200,
                            Height = 30,
                            Text = $"open {popoverAnchor}",
                            Anchor = popoverAnchor,
                            Origin = popoverAnchor,
                            CreateContent = creationFunc
                        };
                    }
                }
            });

        private class DrawableWithPopover : CircularContainer, IHasPopover
        {
            public Func<DrawableWithPopover, Popover> CreateContent { get; set; }

            public string Text
            {
                set => spriteText.Text = value;
            }

            private readonly SpriteText spriteText;

            public DrawableWithPopover()
            {
                Masking = true;
                BorderThickness = 4;
                BorderColour = FrameworkColour.YellowGreenDark;

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDark
                    },
                    spriteText = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = FontUsage.Default.With(italics: true)
                    }
                };
            }

            public Popover GetPopover() => CreateContent.Invoke(this);
        }
    }
}
