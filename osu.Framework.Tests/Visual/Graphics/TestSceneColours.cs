// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osuTK;

namespace osu.Framework.Tests.Visual.Graphics
{
    public partial class TestSceneColours : FrameworkTestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            FillFlowContainer flow;

            Child = new TooltipContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = flow = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Full,
                    Padding = new MarginPadding(10),
                    Spacing = new Vector2(5)
                }
            };

            var colours = typeof(Colour4).GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.GetProperty)
                                         .Where(property => property.PropertyType == typeof(Colour4))
                                         .Select(property => (property.Name, (Colour4)property.GetMethod!.Invoke(null, null)!));

            flow.ChildrenEnumerable = colours.Select(colour => new ColourBox(colour.Name, colour.Item2));
        }

        private partial class ColourBox : Box, IHasTooltip
        {
            public ColourBox(string name, Colour4 colour)
            {
                Colour = colour;
                Name = name;
                Size = new Vector2(50);
            }

            public LocalisableString TooltipText => Name;
        }
    }
}
