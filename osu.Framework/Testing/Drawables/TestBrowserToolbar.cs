// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing.Drawables.Sections;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables
{
    internal class TestBrowserToolbar : CompositeDrawable
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
                    Colour = new Color4(0.1f, 0.1f, 0.1f, 1),
                },
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
                            new Dimension(GridSizeMode.AutoSize),
                        },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                                assemblySection = new ToolbarAssemblySection { RelativeSizeAxes = Axes.Y },
                                new ToolbarRateSection { RelativeSizeAxes = Axes.Both },
                                new ToolbarRecordSection { RelativeSizeAxes = Axes.Y }
                            }
                        },
                    },
                }
            };
        }

        public void AddAssembly(string name, Assembly assembly) => assemblySection.AddAssembly(name, assembly);
    }
}
