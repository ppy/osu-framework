// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Visualisation;

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// Tracks game statistics on a global.
    /// </summary>
    [Cached(typeof(IGlobalStatisticsTracker))]
    internal class GlobalStatisticsDisplay : ToolWindow, IGlobalStatisticsTracker
    {
        private readonly FillFlowContainer<StatisticsGroup> groups;

        public GlobalStatisticsDisplay()
            : base("Global Statistics", "(Ctrl+F2 to toggle)")
        {
            ScrollContent.Children = new Drawable[]
            {
                groups = new FillFlowContainer<StatisticsGroup>
                {
                    Padding = new MarginPadding(5),
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                },
            };
        }

        public void Register(IGlobalStatistic stat) => Schedule(() =>
        {
            var group = groups.FirstOrDefault(g => g.GroupName == stat.Group);

            if (group == null)
                groups.Add(group = new StatisticsGroup(stat.Group));
            group.Add(stat);
        });

        private class StatisticsGroup : CompositeDrawable
        {
            public string GroupName { get; }

            private readonly FillFlowContainer items;

            public StatisticsGroup(string groupName)
            {
                GroupName = groupName;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChildren = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = GroupName,
                                Font = FrameworkFont.Regular.With(weight: "Bold")
                            },
                            items = new FillFlowContainer
                            {
                                Padding = new MarginPadding { Left = 5 },
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                            },
                        }
                    },
                };
            }

            public void Add(IGlobalStatistic stat) => items.Add(new StatisticsItem(stat));

            private class StatisticsItem : CompositeDrawable
            {
                public StatisticsItem(IGlobalStatistic stat)
                {
                    SpriteText valueText;

                    RelativeSizeAxes = Axes.X;
                    AutoSizeAxes = Axes.Y;

                    InternalChildren = new Drawable[]
                    {
                        new SpriteText
                        {
                            Font = FrameworkFont.Regular,
                            Colour = FrameworkColour.Yellow,
                            Text = stat.Name,
                            RelativeSizeAxes = Axes.X,
                            Width = 0.68f,
                        },
                        valueText = new SpriteText
                        {
                            Font = FrameworkFont.Condensed,
                            RelativePositionAxes = Axes.X,
                            X = 0.7f,
                            RelativeSizeAxes = Axes.X,
                            Width = 0.3f,
                        },
                    };

                    stat.DisplayValue.BindValueChanged(val => valueText.Text = val.NewValue, true);
                }
            }
        }
    }
}
