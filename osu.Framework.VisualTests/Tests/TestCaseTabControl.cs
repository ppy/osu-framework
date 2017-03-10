// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.UserInterface.Tab;
using osu.Framework.Screens.Testing;
using OpenTK;

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
                Position = new Vector2(100, 75),
                Width = 200,
            };

            StyledTabControl pinnedAndAutoSort = new StyledTabControl("test 6", "test 9")
            {
                Position = new Vector2(400, 75),
                Width = 200,
                AutoSort = true
            };
            Add(simpleTabcontrol);
            Add(pinnedAndAutoSort);
        }

        private class StyledTabControl : TabControl<string>
        {
            protected override TabDropDownMenu<string> CreateDropDownMenu() => new StyledDropDownMenu();

            protected override TabItem<string> CreateTabItem(string value) => new StyledTabItem { Value = value };

            public StyledTabControl(params string[] pinned) : base(pinned)
            {
            }
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

            protected override IEnumerable<DropDownMenuItem<string>> GetDropDownItems(IEnumerable<KeyValuePair<string, string>> values) =>
                values.Select(v => new StyledDropDownMenuItem(v.Key));

            public StyledDropDownMenu()
            {
                MaxDropDownHeight = int.MaxValue;
                ContentContainer.CornerRadius = 4;
                ScrollContainer.ScrollDraggerVisible = false;
                string[] testItems = new string[10];
                for (int i = 0; i < 10; i++)
                    testItems[i] = @"test " + i;
                Items = testItems.Select(i => new KeyValuePair<string, string>(i, i));
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
