// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTabControl : TestCase
    {
        public override string Description => @"Tab control";

        public override void Reset()
        {
            base.Reset();

            List<KeyValuePair<string, TestEnum>> items = new List<KeyValuePair<string, TestEnum>>();
            foreach (var val in (TestEnum[])Enum.GetValues(typeof(TestEnum)))
                items.Add(new KeyValuePair<string, TestEnum>(val.GetDescription(), val));

            StyledTabControl simpleTabcontrol = new StyledTabControl
            {
                Position = new Vector2(200, 50),
                Size = new Vector2(200, 30),
            };
            items.AsEnumerable().ForEach(item => simpleTabcontrol.AddItem(item.Value));

            StyledTabControl pinnedAndAutoSort = new StyledTabControl
            {
                Position = new Vector2(500, 50),
                Size = new Vector2(200, 30),
                AutoSort = true
            };
            items.GetRange(0, 7).AsEnumerable().ForEach(item => pinnedAndAutoSort.AddItem(item.Value));
            pinnedAndAutoSort.PinItem(TestEnum.Test5);

            Add(simpleTabcontrol);
            Add(pinnedAndAutoSort);

            var nextTest = new Func<TestEnum>(() => items.AsEnumerable()
                                                         .Select(item => item.Value)
                                                         .FirstOrDefault(test => !pinnedAndAutoSort.Items.Contains(test)));

            Stack<TestEnum> pinned = new Stack<TestEnum>();

            AddStep("AddItem", () =>
            {
                var item = nextTest.Invoke();
                if (!pinnedAndAutoSort.Items.Contains(item))
                    pinnedAndAutoSort.AddItem(item);
            });

            AddStep("PinItem", () =>
            {
                var item = nextTest.Invoke();

                if (!pinnedAndAutoSort.Items.Contains(item))
                {
                    pinned.Push(item);
                    pinnedAndAutoSort.AddItem(item);
                    pinnedAndAutoSort.PinItem(item);
                }
            });

            AddStep("UnpinItem", () =>
            {
                if (pinned.Count > 0) pinnedAndAutoSort.UnpinItem(pinned.Pop());
            });
        }

        private class StyledTabControl : TabControl<TestEnum>
        {
            protected override Dropdown<TestEnum> CreateDropdown() => new StyledDropdown();

            protected override TabItem<TestEnum> CreateTabItem(TestEnum value) => new StyledTabItem(value);
        }

        private class StyledTabItem : TabItem<TestEnum>
        {
            private readonly SpriteText text;

            public StyledTabItem(TestEnum value) : base(value)
            {
                AutoSizeAxes = Axes.Both;
                Children = new Drawable[]
                {
                    text = new SpriteText
                    {
                        Margin = new MarginPadding(2),
                        Text = value.ToString(),
                        TextSize = 18
                    }
                };
            }

            protected override void OnActivated() => text.Colour = Color4.MediumPurple;

            protected override void OnDeactivated() => text.Colour = Color4.White;
        }

        private class StyledDropdown : Dropdown<TestEnum>
        {
            protected override Menu CreateMenu() => new Menu();

            protected override DropdownHeader CreateHeader() => new StyledDropdownHeader();

            protected override DropdownMenuItem<TestEnum> CreateMenuItem(string key, TestEnum value) => new StyledDropdownMenuItem(key, value);

            public StyledDropdown()
            {
                DropdownMenu.MaxHeight = int.MaxValue;
                DropdownMenu.CornerRadius = 4;
                DropdownMenu.ScrollContainer.ScrollbarVisible = false;

                DropdownMenu.Anchor = Anchor.TopRight;
                DropdownMenu.Origin = Anchor.TopRight;
                Header.Anchor = Anchor.TopRight;
                Header.Origin = Anchor.TopRight;
            }
        }

        private class StyledDropdownHeader : DropdownHeader
        {
            protected override string Label { get; set; }

            public StyledDropdownHeader()
            {
                Background.Hide(); // don't need a background

                RelativeSizeAxes = Axes.None;
                AutoSizeAxes = Axes.X;

                Foreground.RelativeSizeAxes = Axes.None;
                Foreground.AutoSizeAxes = Axes.Both;

                Foreground.Children = new[]
                {
                    new Box { Width = 20, Height = 20 }
                };
            }
        }

        private class StyledDropdownMenuItem : DropdownMenuItem<TestEnum>
        {
            public StyledDropdownMenuItem(string text, TestEnum value)
                : base(text, value)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = text },
                };
            }
        }

        private enum TestEnum
        {
            Test0,
            Test1,
            Test2,
            Test3,
            Test4,
            Test5,
            Test6,
            Test7,
            Test8,
            Test9,
            Test10,
            Test11,
            Test12
        }
    }
}
