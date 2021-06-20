// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
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
        private PopoverContainer popoverContainer;
        private GridContainer gridContainer;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create popover container", () =>
            {
                Child = popoverContainer = new PopoverContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = gridContainer = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both
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
                InputManager.MoveMouseTo(this.ChildrenOfType<ButtonWithPopover>().First());
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
                InputManager.MoveMouseTo(this.ChildrenOfType<ButtonWithPopover>().Last());
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("popover removed", () => !this.ChildrenOfType<Popover>().Any());
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
            ButtonWithPopover button = null;

            AddStep("add button", () => popoverContainer.Child = button = new ButtonWithPopover
            {
                Width = 200,
                Height = 30,
                RelativePositionAxes = Axes.Both,
                Text = "open",
                Action = () => { },
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
                if (button != null)
                    button.X = x;
            });

            AddSliderStep("move Y", 0f, 1, 0, y =>
            {
                if (button != null)
                    button.Y = y;
            });
        }

        private void createContent(Func<ButtonWithPopover, Popover> creationFunc)
            => AddStep("create content", () =>
            {
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        Anchor popoverAnchor = 0;
                        popoverAnchor |= (Anchor)((int)Anchor.x0 << i);
                        popoverAnchor |= (Anchor)((int)Anchor.y0 << j);

                        cells[j, i].Child = new ButtonWithPopover
                        {
                            Width = 200,
                            Height = 30,
                            Text = $"open {popoverAnchor}",
                            Action = () => { },
                            Anchor = popoverAnchor,
                            Origin = popoverAnchor,
                            CreateContent = creationFunc
                        };
                    }
                }
            });

        private class ButtonWithPopover : BasicButton, IHasPopover
        {
            public Func<ButtonWithPopover, Popover> CreateContent { get; set; }

            public Popover GetPopover() => CreateContent.Invoke(this);
        }
    }
}
