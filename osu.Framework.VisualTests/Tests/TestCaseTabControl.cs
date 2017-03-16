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
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens.Testing;

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

            AddButton("AddItem", () => pinnedAndAutoSort.AddItem(nextTest.Invoke()));
            AddButton("PinItem", () =>
            {
                var test = nextTest.Invoke();
                pinnedAndAutoSort.AddItem(test);
                pinnedAndAutoSort.PinItem(test);
            });
            AddButton("UnpinItem", () => pinnedAndAutoSort.UnpinItem(pinnedAndAutoSort.Items.First()));
        }

        private class StyledTabControl : TabControl<TestEnum>
        {
            protected override DropDownMenu<TestEnum> CreateDropDownMenu() => new StyledDropDownMenu();

            protected override TabItem<TestEnum> CreateTabItem(TestEnum value) => new StyledTabItem { Value = value };
        }

        private class StyledTabItem : TabItem<TestEnum>
        {
            private SpriteText text;
            public new TestEnum Value
            {
                get { return base.Value; }
                set
                {
                    base.Value = value;
                    text.Text = value.ToString();
                }
            }

            public override bool Active
            {
                get { return base.Active; }
                set
                {
                    if (value)
                        fadeActive();
                    else
                        fadeInactive();
                    base.Active = value;
                }
            }

            private void fadeActive()
            {
                text.Colour = Color4.MediumPurple;
            }

            private void fadeInactive()
            {
                text.Colour = Color4.White;
            }

            public StyledTabItem()
            {
                AutoSizeAxes = Axes.Both;
                Children = new Drawable[]
                {
                    text = new SpriteText
                    {
                        Margin = new MarginPadding(2),
                        TextSize = 18
                    }
                };
            }
        }

        private class StyledDropDownMenu : DropDownMenu<TestEnum>
        {
            protected override DropDownHeader CreateHeader() => new StyledDropDownHeader();

            protected override DropDownMenuItem<TestEnum> CreateDropDownItem(string key, TestEnum value) => new StyledDropDownMenuItem(key, value);

            public StyledDropDownMenu()
            {
                MaxDropDownHeight = int.MaxValue;
                ContentContainer.CornerRadius = 4;
                ScrollContainer.ScrollDraggerVisible = false;

                ContentContainer.Anchor = Anchor.TopRight;
                ContentContainer.Origin = Anchor.TopRight;
                Header.Anchor = Anchor.TopRight;
                Header.Origin = Anchor.TopRight;
            }

            protected override void AnimateOpen()
            {
                ContentContainer.Show();
            }

            protected override void AnimateClose()
            {
                ContentContainer.Hide();
            }
        }

        private class StyledDropDownHeader : DropDownHeader
        {
            protected override string Label { get; set; }

            public StyledDropDownHeader()
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

        private class StyledDropDownMenuItem : DropDownMenuItem<TestEnum>
        {
            public StyledDropDownMenuItem(string text, TestEnum value) : base(text, value)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = text },
                };
            }
        }

        private enum TestEnum { Test0, Test1, Test2, Test3, Test4, Test5, Test6, Test7, Test8, Test9, Test10, Test11, Test12 }
    }
}
