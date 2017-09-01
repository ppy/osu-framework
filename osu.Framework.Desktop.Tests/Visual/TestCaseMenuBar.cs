// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Desktop.Tests.Visual
{
    internal class TestCaseMenuBar : TestCase
    {
        private const int max_depth = 5;
        private const int max_count = 5;

        private readonly Random rng;

        public TestCaseMenuBar()
        {
            rng = new Random(1337);

            Add(new StyledMenuBar
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                State = MenuState.Opened,
                AlwaysOpen = true,
                RequireClickToOpen = true,
                Items = new[]
                {
                    generateRandomMenu("First"),
                    generateRandomMenu("Second"),
                    generateRandomMenu("Third"),
                }
            });
        }

        private MenuItem generateRandomMenu(string name = "Menu Item", int currDepth = 1)
        {
            var item = new MenuItem(name);

            if (currDepth == max_depth)
                return item;

            int subCount = rng.Next(0, max_count);
            var subItems = new List<MenuItem>();
            for (int i = 0; i < subCount; i++)
                subItems.Add(generateRandomMenu(item.Text + $" #{i + 1}", currDepth + 1));

            item.Items = subItems;
            return item;
        }

        private class StyledMenuBar : Menu
        {
            public StyledMenuBar()
                : base(Direction.Horizontal)
            {
            }

            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new StyledMenuBarItem(item);

            private class StyledMenuBarItem : DrawableMenuItem
            {
                public StyledMenuBarItem(MenuItem item)
                    : base(item)
                {
                }

                private class StyledMenuBarItemMenu : Menu
                {
                    public StyledMenuBarItemMenu()
                        : base(Direction.Vertical)
                    {
                        Anchor = Anchor.BottomLeft;
                    }

                    protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new StyledMenuItem(item);

                    private class StyledMenuItem : DrawableMenuItem
                    {
                        public StyledMenuItem(MenuItem item)
                            : base(item)
                        {
                        }
                    }
                }
            }
        }
    }
}
