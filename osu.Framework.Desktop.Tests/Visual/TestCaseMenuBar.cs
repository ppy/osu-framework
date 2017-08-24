// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Desktop.Tests.Visual
{
    internal class TestCaseMenuBar : TestCase
    {
        public TestCaseMenuBar()
        {
            Add(new MenuBar
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Items = new[]
                {
                    new MenuBarItem("Menu Bar Item 1")
                    {
                        Items = new[]
                        {
                            new ContextMenuItem("Menu Bar Item 1, Item 1"),
                            new ContextMenuItem("Menu Bar Item 1, Item 2"),
                            new ContextMenuItem("Menu Bar Item 1, Item 3"),
                            new ContextMenuItem("Menu Bar Item 1, Item 4"),
                        }
                    },
                    new MenuBarItem("Menu Bar Item 2")
                    {
                        Items = new[]
                        {
                            new ContextMenuItem("Menu Bar Item 2, Item 1"),
                            new ContextMenuItem("Menu Bar Item 2, Item 2"),
                            new ContextMenuItem("Menu Bar Item 2, Item 3"),
                            new ContextMenuItem("Menu Bar Item 2, Item 4"),
                            new ContextMenuItem("Menu Bar Item 2, Item 5"),
                            new ContextMenuItem("Menu Bar Item 2, Item 6"),
                            new ContextMenuItem("Menu Bar Item 2, Item 7"),
                            new ContextMenuItem("Menu Bar Item 2, Item 8"),
                            new ContextMenuItem("Menu Bar Item 2, Item 9"),
                            new ContextMenuItem("Menu Bar Item 2, Item 10"),
                        }
                    },
                    new MenuBarItem("Menu Bar Item 3")
                    {
                        Items = new[]
                        {
                            new ContextMenuItem("Menu Bar Item 3, Item 1"),
                        }
                    },
                    new MenuBarItem("Menu Bar Item 4")
                }
            });
        }
    }
}
