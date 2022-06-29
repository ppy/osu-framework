// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestScenePopover : GridTestScene
    {
        public TestScenePopover()
            : base(3, 3)
        {
        }

        [Test]
        public void TestSimpleText() => createContent((anchor, popover) =>
        {
            popover.Child = new SpriteText
            {
                Text = $"{anchor} popover"
            };
        });

        [Test]
        public void TestSizingDirectly() => createContent((_, popover) =>
        {
            popover.Size = new Vector2(200, 100);

            popover.Child = new SpriteText
            {
                Text = "I have a custom size!"
            };
        });

        [Test]
        public void TestInteractiveContent() => createContent((anchor, popover) =>
        {
            TextBox textBox;

            popover.AlwaysPresent = true;
            popover.Child = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                Width = 200,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    textBox = new BasicTextBox
                    {
                        PlaceholderText = $"{anchor} text box",
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
            };
        });

        private void createContent(Action<Anchor, BasicPopover> creationFunc)
            => AddStep("create content", () =>
            {
                for (int i = 0; i < 3; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        Anchor popoverAnchor = 0;
                        popoverAnchor |= (Anchor)((int)Anchor.x0 << i);
                        popoverAnchor |= (Anchor)((int)Anchor.y0 << j);

                        var popover = new BasicPopover
                        {
                            PopoverAnchor = popoverAnchor,
                            State = { Value = Visibility.Visible }
                        };
                        creationFunc.Invoke(popoverAnchor, popover);

                        Cell(j, i).Children = new Drawable[]
                        {
                            new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Origin = popoverAnchor,
                                Anchor = popoverAnchor,
                                Child = popover
                            }
                        };
                    }
                }
            });
    }
}
