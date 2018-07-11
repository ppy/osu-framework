// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class Toolbar : CompositeDrawable
    {
        private ToolbarAssemblySection assemblySection;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Relative, 0.5f),
                        new Dimension(GridSizeMode.Relative, 0.5f),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new Container
                            {
                                Padding = new MarginPadding(10),
                                RelativeSizeAxes = Axes.Both,
                                Child = new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.AutoSize),
                                        new Dimension(GridSizeMode.Distributed),
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            assemblySection = new ToolbarAssemblySection { RelativeSizeAxes = Axes.Y },
                                            new ToolbarRateSection { RelativeSizeAxes = Axes.Both }
                                        }
                                    },
                                },
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                Padding = new MarginPadding { Top = 0, Bottom = 10, Left = 10, Right = 10 },
                                RelativeSizeAxes = Axes.Both,
                                Child = new ToolbarPlaybackSection { RelativeSizeAxes = Axes.Y }
                            }
                        }
                    }
                }
            };
        }

        public void AddAssembly(string name, Assembly assembly) => assemblySection.AddAssembly(name, assembly);
    }
}
