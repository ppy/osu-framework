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

        private DropDownMenu<BeatmapExample> dropDownMenu;
        private StyledDropDownMenu styledDropDownMenu;
        private SpriteText labelSelectedMap, labelNewMap;

        public override void Reset()
        {
            base.Reset();

            // Creating drop-down for beatmaps
            dropDownMenu = new DropDownMenu<BeatmapExample>
            {
                Width = 150,
                Position = new Vector2(200, 20),
                Description = @"Beatmap selector example",
                Items = new DropDownMenuItem<BeatmapExample>[]
                {
                    new DropDownMenuHeader<BeatmapExample>("Ranked"),
                    new DropDownMenuItem<BeatmapExample>(
                        "Example",
                        new BeatmapExample
                        {
                            Name = @"Example",
                            Mapper = @"peppy",
                            Status = BeatmapExampleStatus.Ranked,
                        }
                    ),
                    new DropDownMenuItem<BeatmapExample>(
                        "Make up!",
                        new BeatmapExample
                        {
                            Name = @"Make up!",
                            Mapper = @"peppy",
                            Status = BeatmapExampleStatus.Ranked,
                        }
                    ),
                    new DropDownMenuItem<BeatmapExample>(
                        "Platinum",
                        new BeatmapExample
                        {
                            Name = @"Platinum",
                            Mapper = @"arflyte",
                            Status = BeatmapExampleStatus.Ranked,
                        }
                    ),
                },
            };
            Add(dropDownMenu);

            // Adding more items after init
            dropDownMenu.AddItem(new DropDownMenuHeader<BeatmapExample>("Not submitted"));
            dropDownMenu.AddItem(new DropDownMenuItem<BeatmapExample>(
                "Lorem ipsum dolor sit amed",
                new BeatmapExample
                {
                    Name = @"Lorem ipsum dolor sit amed",
                    Mapper = @"Plato",
                    Status = BeatmapExampleStatus.NotSubmitted,
                })
            );

            // Setting default index and event handler
            dropDownMenu.SelectedIndex = 0;
            dropDownMenu.ValueChanged += DropDownBox_ValueChanged;

            labelSelectedMap = new SpriteText
            {
                Text = @"Waiting for map...",
                Position = new Vector2(450, 20),
            };
            Add(labelSelectedMap);

            labelNewMap = new SpriteText
            {
                Text = @"",
                Position = new Vector2(450, 60),
            };
            Add(labelNewMap);

            // Styled drop-down example
            StyledDropDownMenuItem[] testItems = new StyledDropDownMenuItem[10];
            for (int i = 0; i < 10; i++)
                testItems[i] = new StyledDropDownMenuItem(@"test " + i);
            styledDropDownMenu = new StyledDropDownMenu
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Description = @"Drop-down menu with style",
                Depth = 1,
                Items = testItems,
                SelectedIndex = 4,
            };
            Add(styledDropDownMenu);

            AddButton(@"+ beatmap", delegate
            {
                string[] mapNames = { "Cool", "Stylish", "Philosofical", "Tekno" };
                string[] mapperNames = { "peppy", "arflyte", "Plato", "BanchoBot" };

                BeatmapExample newMap = new BeatmapExample
                {
                    Name = mapNames[RNG.Next(mapNames.Length)],
                    Mapper = mapperNames[RNG.Next(mapperNames.Length)],
                    Status = BeatmapExampleStatus.NotSubmitted,
                };

                dropDownMenu.AddItem(new DropDownMenuItem<BeatmapExample>(newMap.Name, newMap));
                labelNewMap.Text = $@"Added ""{newMap.Name}"" by {newMap.Mapper} as {newMap.Status}";
            });
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

            public StyledDropDownComboBox()
            {
                Foreground.Padding = new MarginPadding(4);
            }
        }

        private class StyledDropDownMenuItem : DropDownMenuItem<string>
        {
            public StyledDropDownMenuItem(string text) : base(text, text)
            {
                AutoSizeAxes = Axes.None;
                Height = 0;
                Foreground.Padding = new MarginPadding(2);
            }

            protected override void OnSelectChange()
            {
                if (!IsLoaded)
                    return;

                FormatBackground();
                FormatCaret();
                FormatLabel();
            }

            protected override void FormatCaret()
            {
                (Caret as SpriteText).Text = IsSelected ? @"+" : @"-";
            }

            protected override void FormatLabel()
            {
                if (IsSelected)
                    (Label as SpriteText).Text = @"*" + Value + @"*";
                else
                    (Label as SpriteText).Text = Value.ToString();
            }
        }

        public class DropDownMenuHeader<T> : DropDownMenuItem<T>
        {
            public override bool CanSelect => false;
            protected override Color4 BackgroundColour => Color4.Blue;
            protected override Color4 BackgroundColourHover => BackgroundColour;

            public DropDownMenuHeader(string text) : base(text, default(T))
            {
            }
        }

        public void DropDownBox_ValueChanged(object sender, System.EventArgs e)
        {
            BeatmapExample ex = (sender as DropDownMenu<BeatmapExample>).SelectedValue;
            labelSelectedMap.Text = $@"You've selected ""{ex.Name}"", mapped by {ex.Mapper}";
        }

        public class BeatmapExample
        {
            public string Name { get; set; }
            public string Mapper { get; set; }
            public BeatmapExampleStatus Status { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public enum BeatmapExampleStatus
        {
            Ranked,
            Approved,
            Qualified,
            Loved,
            Pending,
            NotSubmitted,
        }
    }
}