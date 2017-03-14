// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.UserInterface.Tab;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseTabControl : TestCase
    {
        public override string Description => @"Tab control";

        public override void Reset()
        {
            base.Reset();

            StyledTabControl simpleTabcontrol = new StyledTabControl
            {
                Position = new Vector2(200, 50),
                Width = 200,
            };
            int i = 0;
            while (i < 10)
                simpleTabcontrol.AddTab(@"test " + i++);

            StyledTabControl pinnedAndAutoSort = new StyledTabControl
            {
                Position = new Vector2(500, 50),
                Width = 200,
                AutoSort = true
            };

            i = 0;
            while (i < 10)
                pinnedAndAutoSort.AddTab(@"test " + i++);
            pinnedAndAutoSort.Tabs.Skip(6).First().Pinned = true;

            Add(simpleTabcontrol);
            Add(pinnedAndAutoSort);

            AddButton("AddItem", () => pinnedAndAutoSort.AddTab(@"test " + i++));
            AddButton("PinItem", () => pinnedAndAutoSort.AddTab(@"test " + i++).Pinned =true);
        }

        private class StyledTabControl : TabControl<string>
        {
            protected override TabDropDownMenu<string> CreateDropDownMenu() => new StyledDropDownMenu();

            protected override TabItem<string> CreateTabItem(string value) => new StyledTabItem { Value = value };
        }

        private class StyledTabItem : TabItem<string>
        {
            private SpriteText text;
            public new string Value
            {
                get { return base.Value; }
                set
                {
                    base.Value = value;
                    text.Text = value;
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

        private class StyledDropDownMenu : TabDropDownMenu<string>
        {
            public override float HeaderWidth => 20;
            public override float HeaderHeight => 20;

            protected override DropDownHeader CreateHeader() => new StyledDropDownHeader();

            protected override DropDownMenuItem<string> CreateDropDownItem(string key, string value) => new StyledDropDownMenuItem(key);

            public StyledDropDownMenu()
            {
                MaxDropDownHeight = int.MaxValue;
                ContentContainer.CornerRadius = 4;
                ScrollContainer.ScrollDraggerVisible = false;
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

        private class StyledDropDownHeader : TabDropDownHeader
        {
            protected override string Label { get; set; }

            public StyledDropDownHeader()
            {
                Foreground.Children = new[]
                {
                    new Box { Width = 20, Height = 20 }
                };
            }
        }

        private class StyledDropDownMenuItem : DropDownMenuItem<string>
        {
            public StyledDropDownMenuItem(string text) : base(text, text)
            {
                AutoSizeAxes = Axes.Y;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    new SpriteText { Text = text },
                };
            }
        }
    }
}
