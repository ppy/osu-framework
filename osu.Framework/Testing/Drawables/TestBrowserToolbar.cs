// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing.Drawables.Sections;

namespace osu.Framework.Testing.Drawables
{
    internal class TestBrowserToolbar : CompositeDrawable
    {
        private ToolbarAssemblySection assemblySection;

        [BackgroundDependencyLoader]
        private void load()
        {
            const float section_padding = 10;

            InternalChildren = new Drawable[]
            {
                new SafeAreaContainer
                {
                    SafeAreaOverrideEdges = Edges.Top | Edges.Right,
                    RelativeSizeAxes = Axes.Both,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.GreenDark,
                    },
                },
                new Container
                {
                    Padding = new MarginPadding(section_padding),
                    RelativeSizeAxes = Axes.Both,
                    Child = new GridContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ColumnDimensions = new[]
                        {
                            new Dimension(GridSizeMode.AutoSize),
                            new Dimension(),
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                assemblySection = new ToolbarAssemblySection { RelativeSizeAxes = Axes.Y },
                                new ToolbarRateSection { RelativeSizeAxes = Axes.Both },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Margin = new MarginPadding { Left = section_padding },
                                    Children = new Drawable[]
                                    {
                                        new Container //Backdrop of the record section
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding(-section_padding),
                                            Child = new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = FrameworkColour.GreenDarker,
                                            },
                                        },
                                        new ToolbarRecordSection { RelativeSizeAxes = Axes.Y }
                                    }
                                },
                            }
                        },
                    },
                }
            };
        }

        public void AddAssembly(string name, Assembly assembly) => assemblySection.AddAssembly(name, assembly);
    }
}
