// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
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
                            Children = new Drawable[]
                            {
                                new ClickableContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            Colour = Color4.Blue,
                                            RelativeSizeAxes = Axes.Both,
                                        },
                                        new TextFlowContainer
                                        {
                                            AutoSizeAxes = Axes.X,
                                            TextAnchor = Anchor.TopCentre,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Text = "click blocking container between\nPopover creator and PopoverContainer"
                                        }
                                    }
                                },
                                gridContainer = new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both
                                }
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
        public void TestShowHide()
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
            AddAssert("popover shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));

            AddStep("click popover", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<Popover>().Single().Body);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover still visible", () => this.ChildrenOfType<Popover>().Single().State.Value == Visibility.Visible);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().First().ScreenSpaceDrawQuad.BottomRight + new Vector2(10));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("all hidden", () => this.ChildrenOfType<Popover>().All(popover => popover.State.Value != Visibility.Visible));
        }

        [Test]
        public void TestHideViaKeyboard()
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
            AddAssert("popover shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));

            AddStep("press Escape", () => InputManager.Key(Key.Escape));
            AddAssert("all hidden", () => this.ChildrenOfType<Popover>().All(popover => popover.State.Value != Visibility.Visible));
        }

        [Test]
        public void TestShowHideViaExtensionMethod()
        {
            createContent(button => new BasicPopover
            {
                Child = new SpriteText
                {
                    Text = $"{button.Anchor} popover"
                }
            });

            AddStep("show popover manually", () => this.ChildrenOfType<DrawableWithPopover>().First().ShowPopover());
            AddAssert("popover shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));

            AddStep("hide popover manually", () => popoverContainer.HidePopover());
            AddAssert("all hidden", () => this.ChildrenOfType<Popover>().All(popover => popover.State.Value != Visibility.Visible));
        }

        [Test]
        public void TestClickBetweenMultiple()
        {
            createContent(button => new BasicPopover
            {
                Name = button.Anchor.ToString(),
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

            AddAssert("first shown", () => this.ChildrenOfType<Popover>().Single().Name == Anchor.TopLeft.ToString());

            AddStep("click last button", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().Last());
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("last shown", () => this.ChildrenOfType<Popover>().Single().Name == Anchor.BottomRight.ToString());
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

            AddAssert("popover shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));

            AddStep("mousedown popover", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<Popover>().Single().Body);
                InputManager.PressButton(MouseButton.Left);
            });
            AddAssert("popover still visible", () => this.ChildrenOfType<Popover>().Single().State.Value == Visibility.Visible);

            AddStep("move away", () => InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().Last()));

            AddStep("release button", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("popover remains", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));
        }

        [Test]
        public void TestInteractiveContent()
        {
            createContent(button =>
            {
                TextBox textBox;

                return new AnimatedPopover
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

            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<DrawableWithPopover>().First());
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("popover shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));

            AddStep("click textbox", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<TextBox>().First());
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("textbox is focused", () => InputManager.FocusedDrawable is TextBox);
            AddAssert("popover still shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));
            AddStep("click in popover", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<Popover>().First().Body.ScreenSpaceDrawQuad.TopLeft + Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("popover is focused", () => InputManager.FocusedDrawable is Popover);
            AddAssert("popover still shown", () => this.ChildrenOfType<Popover>().Any(popover => popover.State.Value == Visibility.Visible));
        }

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

        [Test]
        public void TestAutoSize()
        {
            AddStep("create content", () =>
            {
                popoverWrapper.RelativeSizeAxes = popoverContainer.RelativeSizeAxes = Axes.X;
                popoverWrapper.AutoSizeAxes = popoverContainer.AutoSizeAxes = Axes.Y;

                popoverContainer.Child = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 200,
                    Child = new DrawableWithPopover
                    {
                        Width = 200,
                        Height = 30,
                        Text = "open",
                        CreateContent = _ => new BasicPopover
                        {
                            Child = new SpriteText
                            {
                                Text = "I'm in an auto-sized container!"
                            }
                        }
                    }
                };
            });

            AddSliderStep("change content height", 100, 500, 200, height =>
            {
                if (popoverContainer?.Children.Count == 1)
                    popoverContainer.Child.Height = height;
            });
        }

        [Test]
        public void TestExternalPopoverControl()
        {
            TextBoxWithPopover target = null;

            AddStep("create content", () =>
            {
                popoverContainer.Child = target = new TextBoxWithPopover
                {
                    Width = 200,
                    Height = 30,
                    PlaceholderText = "focus to show popover"
                };
            });

            AddStep("click text box", () =>
            {
                InputManager.MoveMouseTo(target);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover shown", () => this.ChildrenOfType<Popover>().Any());

            AddStep("take away text box focus", () => InputManager.ChangeFocus(null));
            AddAssert("popover hidden", () => !this.ChildrenOfType<Popover>().Any());
        }

        [Test]
        public void TestPopoverCleanupOnTargetDisposal()
        {
            DrawableWithPopover target = null;

            AddStep("add button", () => popoverContainer.Child = target = new DrawableWithPopover
            {
                Width = 200,
                Height = 30,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = "open",
                CreateContent = _ => new BasicPopover
                {
                    Child = new SpriteText
                    {
                        Text = "This popover should be cleaned up when its button is removed",
                    }
                }
            });

            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(target);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover created", () => this.ChildrenOfType<Popover>().Any());

            AddStep("dispose of button", () => popoverContainer.Clear());
            AddUntilStep("no popover present", () => !this.ChildrenOfType<Popover>().Any());
        }

        [Test]
        public void TestPopoverCleanupOnTargetHide()
        {
            DrawableWithPopover target = null;

            AddStep("add button", () => popoverContainer.Child = target = new DrawableWithPopover
            {
                Width = 200,
                Height = 30,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = "open",
                CreateContent = _ => new BasicPopover
                {
                    Child = new SpriteText
                    {
                        Text = "This popover should be cleaned up when its button is hidden",
                    }
                }
            });

            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(target);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover created", () => this.ChildrenOfType<Popover>().Any());

            AddStep("hide button", () => target.Hide());
            AddUntilStep("no popover present", () => !this.ChildrenOfType<Popover>().Any());
        }

        [Test]
        public void TestPopoverEventHandling()
        {
            EventHandlingContainer eventHandlingContainer = null;
            DrawableWithPopover target = null;

            AddStep("add button", () => popoverContainer.Child = eventHandlingContainer = new EventHandlingContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = target = new DrawableWithPopover
                {
                    Width = 200,
                    Height = 30,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "open",
                    CreateContent = _ => new BasicPopover
                    {
                        Child = new SpriteText
                        {
                            Text = "This popover should be handle hover and click events",
                        }
                    }
                }
            });

            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(target);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("container received hover", () => eventHandlingContainer.HoverReceived);

            AddAssert("popover created", () => this.ChildrenOfType<Popover>().Any());

            AddStep("mouse over popover", () =>
            {
                eventHandlingContainer.Reset();
                InputManager.MoveMouseTo(this.ChildrenOfType<Popover>().Single().Body);
            });

            AddAssert("container did not receive hover", () => !eventHandlingContainer.HoverReceived);

            AddStep("click on popover", () => InputManager.Click(MouseButton.Left));
            AddAssert("container did not receive click", () => !eventHandlingContainer.ClickReceived);

            AddStep("dismiss popover", () =>
            {
                InputManager.MoveMouseTo(eventHandlingContainer.ScreenSpaceDrawQuad.TopLeft + new Vector2(10));
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("container received hover", () => eventHandlingContainer.HoverReceived);
            AddStep("click again", () => InputManager.Click(MouseButton.Left));
            AddAssert("container received click", () => eventHandlingContainer.ClickReceived);
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

        private class AnimatedPopover : BasicPopover
        {
            protected override void PopIn() => this.FadeIn(300, Easing.OutQuint);
            protected override void PopOut() => this.FadeOut(300, Easing.OutQuint);
        }

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

            protected override bool OnClick(ClickEvent e)
            {
                this.ShowPopover();
                return true;
            }
        }

        private class TextBoxWithPopover : BasicTextBox, IHasPopover
        {
            protected override void OnFocus(FocusEvent e)
            {
                base.OnFocus(e);
                this.ShowPopover();
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                this.HidePopover();
            }

            public Popover GetPopover() => new BasicPopover
            {
                Child = new SpriteText
                {
                    Text = "the text box has focus now!"
                }
            };
        }

        private class EventHandlingContainer : Container
        {
            private readonly Box colourBox;

            public bool ClickReceived { get; private set; }
            public bool HoverReceived { get; private set; }

            protected override Container<Drawable> Content { get; }

            public EventHandlingContainer()
            {
                AddInternal(new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        colourBox = new Box
                        {
                            Colour = Color4.Black,
                            RelativeSizeAxes = Axes.Both,
                        },
                        Content = new Container { RelativeSizeAxes = Axes.Both },
                    }
                });
            }

            public void Reset()
            {
                ClickReceived = HoverReceived = false;
                colourBox.FadeColour(Color4.Black);
            }

            protected override bool OnClick(ClickEvent e)
            {
                ClickReceived = true;
                colourBox.FlashColour(Color4.White, 200);
                return true;
            }

            protected override bool OnHover(HoverEvent e)
            {
                HoverReceived = true;
                colourBox.FadeColour(Color4.DarkSlateBlue, 200);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                colourBox.FadeColour(Color4.Black, 200);
                base.OnHoverLost(e);
            }
        }
    }
}
