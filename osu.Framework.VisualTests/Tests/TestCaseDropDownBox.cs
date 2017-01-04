// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using System.Diagnostics;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseDropDownBox : TestCase
    {
        public override string Name => @"Drop-down boxes";

        public override string Description => @"Drop-down boxes";

        private StyledDropDownMenu styledDropDownMenu;

        public override void Reset()
        {
            base.Reset();
            StyledDropDownMenuItem[] testItems = new StyledDropDownMenuItem[10];
            for (int i = 0; i < 10; i++)
                testItems[i] = new StyledDropDownMenuItem(@"test " + i);
            styledDropDownMenu = new StyledDropDownMenu
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Description = @"Drop-down menu",
                Depth = 1,
                Items = testItems,
                SelectedIndex = 4,
            };
            Add(styledDropDownMenu);
        }

        private class StyledDropDownMenu : DropDownMenu<string>
        {
            protected override float DropDownListSpacing => 4;

            protected override DropDownComboBox CreateComboBox()
            {
                return new StyledDropDownComboBox();
            }

            public StyledDropDownMenu()
            {
                ComboBox.CornerRadius = 4;
                DropDown.CornerRadius = 4;
            }

            protected override void AnimateOpen()
            {
                foreach (StyledDropDownMenuItem child in DropDownList.Children)
                {
                    child.FadeIn(200);
                    child.ResizeTo(new Vector2(1, 24), 200);
                }
                DropDown.Show();
            }

            protected override void AnimateClose()
            {
                foreach (StyledDropDownMenuItem child in DropDownList.Children)
                {
                    child.ResizeTo(new Vector2(1, 0), 200);
                    child.FadeOut(200);
                }
            }
        }

        private class StyledDropDownComboBox : DropDownComboBox
        {
            protected override Color4 BackgroundColour => new Color4(255, 255, 255, 100);
            protected override Color4 BackgroundColourHover => Color4.HotPink;

            private SpriteText label;
            protected override string Label
            {
                get { return label.Text; }
                set { label.Text = value; }
            }

            public StyledDropDownComboBox()
            {
                Foreground.Padding = new MarginPadding(4);
                Children = new[]
                {
                    label = new SpriteText(),
                };
            }
        }

        private class StyledDropDownMenuItem : DropDownMenuItem<string>
        {
            private SpriteText caret, label;
        
            public StyledDropDownMenuItem(string text) : base(text, text)
            {
                AutoSizeAxes = Axes.None;
                Height = 0;
                Foreground.Padding = new MarginPadding(2);

                Children = new[]
                {
                    caret = new SpriteText(),
                    label = new SpriteText
                    {
                        Margin = new MarginPadding { Left = 15 },
                    },
                };
                formatCaret();
                formatLabel();
            }

            protected override void OnSelectChange()
            {
                if (!IsLoaded)
                    return;

                FormatBackground();
                formatCaret();
                formatLabel();
            }

            private void formatCaret()
            {
                caret.Text = IsSelected ? @"+" : @"-";
            }

            private void formatLabel()
            {
                if (IsSelected)
                    label.Text = @"*" + Value + @"*";
                else
                    label.Text = Value.ToString();
            }
        }
    }
}