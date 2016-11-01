// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using System.Collections.ObjectModel;
using System;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseDropDownBox : TestCase
    {
        public override string Name => @"Drop-down boxes";

        public override string Description => @"Drop-down boxes";

        private DropDownMenu dropDownMenu;
        private StyledDropDownMenu styledDropDownMenu;
        private SpriteText label;

        public override void Reset()
        {
            base.Reset();

            dropDownMenu = new DropDownMenu
            {
                Width = 150,
                Position = new Vector2(200, 20),
                Description = @"Beatmap selector example",
            };

            ObservableCollection<object> maps = new ObservableCollection<object>();

            maps.Add(new BeatmapExample
            {
                Name = @"Example name",
                Mapper = @"Example Mapper",
                Status = @"Ranked",
            });

            maps.Add(new BeatmapExample
            {
                Name = @"Make up!",
                Mapper = @"peppy",
                Status = @"Ranked",
            });

            maps.Add(new BeatmapExample
            {
                Name = @"Platinum",
                Mapper = @"arflyte",
                Status = @"Loved",
            });

            maps.Add(new BeatmapExample
            {
                Name = @"Lorem ipsum dolor sit amed",
                Mapper = @"Plato",
                Status = @"Ranked",
            });

            dropDownMenu.Items = maps;

            dropDownMenu.ValueChanged += DropDownBox_ValueChanged;

            Add(dropDownMenu);

            label = new SpriteText
            {
                Text = @"Waiting for map...",
                Position = new Vector2(450, 20),
            };

            Add(label);

            styledDropDownMenu = new StyledDropDownMenu
            {
                Width = 150,
                Position = new Vector2(200, 70),
                Description = @"Drop-down menu with style",
                Depth = -1,
            };

            string[] testItems = new string[10];
            for (int i = 0; i < 10; i++)
                testItems[i] = @"test " + i;

            styledDropDownMenu.Items = new ObservableCollection<object>(testItems);

            Add(styledDropDownMenu);

            AddButton(@"+ beatmap", delegate
            {
                maps.Add(new BeatmapExample
                {
                    Name = "New beatmap",
                    Mapper = "New mapper",
                    Status = "Not submitted",
                });
            });

            AddButton(@"- beatmap", delegate
            {
                if (maps.Count > 1)
                    maps.RemoveAt(RNG.Next(maps.Count));
            });
        }

        private class StyledDropDownMenu : DropDownMenu
        {
            protected override float DropDownListSpacing => 4;

            protected override Type MenuItemType => typeof(StyledDropDownMenuItem);

            public StyledDropDownMenu()
            {
                ComboBox.CornerRadius = 4;
                ComboBoxLabel.Margin = new MarginPadding(4);
                ComboBoxCaret.Margin = new MarginPadding(4);
            }

            protected override void AnimateOpen()
            {
                foreach (Drawable child in DropDownList.Children)
                {
                    child.MoveToY((child as DropDownMenuItem).ExpectedPositionY, 200, Graphics.Transformations.EasingTypes.In);
                    child.FadeIn(200);
                }
                DropDownList.Show();
            }

            protected override void AnimateClose()
            {
                foreach (Drawable child in DropDownList.Children)
                {
                    child.MoveToY(0, 200, Graphics.Transformations.EasingTypes.In);
                    child.FadeOut(200);
                }
                DropDownList.Delay(200);
                DropDownList.Hide();
                DropDownList.DelayReset();
            }
        }

        private class StyledDropDownMenuItem : DropDownMenuItem
        {
            public StyledDropDownMenuItem(DropDownMenu parent) : base(parent)
            {
                Label.Margin = new MarginPadding(2);
                Caret.Margin = new MarginPadding(2);
            }

            protected override void FormatCaret()
            {
                (Caret as SpriteText).Text = IsSelected ? @"+" : @"-";
            }

            protected override void FormatLabel()
            {
                if (IsSelected)
                    Label.Text = @"*" + Item.ToString() + @"*";
                else
                    Label.Text = Item.ToString();
            }
        }

        public void DropDownBox_ValueChanged(object sender, System.EventArgs e)
        {
            BeatmapExample ex = (BeatmapExample)(sender as DropDownMenu).SelectedItem;
            label.Text = @"You've selected " + ex.Name + @", mapped by " + ex.Mapper;
        }

        public class BeatmapExample
        {
            public string Name;
            public string Mapper;
            public string Status;

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
