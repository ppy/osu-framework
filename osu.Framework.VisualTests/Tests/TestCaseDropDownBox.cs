// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Model;
using osu.Framework.GameModes.Testing;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Lists;
using System;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseDropDownBox : TestCase
    {
        public override string Name => @"Drop-down boxes";

        public override string Description => @"Drop-down boxes";

        private DropDownMenu dropDownMenu;
        private StyledDropDownMenu styledDropDownMenu;
        private SpriteText labelSelectedMap, labelNewMap;

        public override void Reset()
        {
            base.Reset();

            dropDownMenu = new DropDownMenu
            {
                Width = 150,
                Position = new Vector2(200, 20),
                Description = @"Beatmap selector example",
            };

            CollectionView maps = new CollectionView();
            maps.GroupDescriptions.Add(new PropertyDescription
            {
                PropertyName = @"Status",
                StringConverter = item =>
                {
                    switch((item as BeatmapExample).Status)
                    {
                        case BeatmapExampleStatus.Approved:
                            return "Approved";
                        case BeatmapExampleStatus.Loved:
                            return "Loved";
                        case BeatmapExampleStatus.NotSubmitted:
                            return "Not Submitted";
                        case BeatmapExampleStatus.Pending:
                            return "Pending";
                        case BeatmapExampleStatus.Qualified:
                            return "Qualified";
                        case BeatmapExampleStatus.Ranked:
                            return "Ranked";
                        default:
                            return "Unknown";
                    }
                },
            });
            maps.SortDescriptions.Add(new PropertyDescription { PropertyName = @"Name" });

            maps.Add(new BeatmapExample
            {
                Name = @"Example name",
                Mapper = @"Example Mapper",
                Status = BeatmapExampleStatus.Ranked,
            });

            maps.Add(new BeatmapExample
            {
                Name = @"Make up!",
                Mapper = @"peppy",
                Status = BeatmapExampleStatus.Ranked,
            });

            maps.Add(new BeatmapExample
            {
                Name = @"Platinum",
                Mapper = @"arflyte",
                Status = BeatmapExampleStatus.Loved,
            });

            maps.Add(new BeatmapExample
            {
                Name = @"Lorem ipsum dolor sit amed",
                Mapper = @"Plato",
                Status = BeatmapExampleStatus.Ranked,
            });

            dropDownMenu.Items = maps;
            dropDownMenu.SelectedIndex = 0;

            dropDownMenu.ValueChanged += DropDownBox_ValueChanged;

            Add(dropDownMenu);

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

            styledDropDownMenu.Items = new CollectionView(testItems);

            Add(styledDropDownMenu);

            AddButton(@"+ beatmap", delegate
            {
                string[] mapNames = { "Cool", "Stylish", "Philosofical", "Tekno"};
                string[] mapperNames = { "peppy", "arflyte", "Plato", "BanchoBot" };

                BeatmapExample newMap = new BeatmapExample
                {
                    Name = mapNames[RNG.Next(mapNames.Length)],
                    Mapper = mapperNames[RNG.Next(mapperNames.Length)],
                    Status = (BeatmapExampleStatus)RNG.Next(Enum.GetValues(typeof(BeatmapExampleStatus)).Length),
                };

                maps.Add(newMap);
                labelNewMap.Text = $@"Added ""{newMap.Name}"" by {newMap.Mapper} as {newMap.Status}";
            });

            AddButton(@"- beatmap", delegate
            {
                if (maps.Count > 1)
                {
                    int toRemove = RNG.Next(maps.Count);

                    labelNewMap.Text = $@"Removed ""{(maps[toRemove] as BeatmapExample).Name}"" by {(maps[toRemove] as BeatmapExample).Mapper}";
                    maps.RemoveAt(toRemove);
                }
            });

            AddButton(@"(un)group by mapper", delegate
            {
                if (maps.GroupDescriptions.Count == 1)
                    maps.GroupDescriptions.Add(new PropertyDescription { PropertyName = "Mapper" });
                else
                    maps.GroupDescriptions.RemoveAt(1);
            });

            AddButton(@"invert group order", delegate
            {
                maps.GroupDescriptions[0].CompareDescending = !maps.GroupDescriptions[0].CompareDescending;
            });
        }

        private class StyledDropDownMenu : DropDownMenu
        {
            protected override float DropDownListSpacing => 4;

            protected override Type ComboBoxType => typeof(StyledDropDownComboBox);
            protected override Type MenuItemType => typeof(StyledDropDownMenuItem);

            public StyledDropDownMenu()
            {
                ComboBox.CornerRadius = 4;
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

        private class StyledDropDownComboBox : DropDownComboBox
        {
            protected override Color4 BackgroundColour => new Color4(255, 255, 255, 100);
            protected override Color4 BackgroundColourHover => Color4.HotPink;

            public StyledDropDownComboBox(DropDownMenu parent) : base(parent)
            {
                Foreground.Padding = new MarginPadding(4);
            }
        }

        private class StyledDropDownMenuItem : DropDownMenuItem
        {
            public StyledDropDownMenuItem(DropDownMenu parent) : base(parent)
            {
                Foreground.Padding = new MarginPadding(2);
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
