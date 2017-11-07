// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.MathUtils;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseGridContainer : TestCase
    {
        public TestCaseGridContainer()
        {
            Add(new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                Definition = new GridDefinition
                {
                    Cells = new[]
                    {
                        new CellDefinition
                        {
                            ColumnSpan = 2,
                            Content = new FillBox()
                        },
                        new CellDefinition
                        {
                            Row = 1,
                            Content = new FillBox()
                        },
                        new CellDefinition
                        {
                            Column = 1,
                            Row = 1,
                            Content = new FillBox()
                        },
                        new CellDefinition
                        {
                            Column = 2,
                            RowSpan = 2,
                            Content = new FillBox()
                        }
                    },
                    Rows = new[]
                    {
                        new RowDefinition(0) { Height = 50 }
                    }
                }
            });
        }

        private class FillBox : Box
        {
            public FillBox()
            {
                RelativeSizeAxes = Axes.Both;
                Colour = new Color4(RNG.NextSingle(1), RNG.NextSingle(1), RNG.NextSingle(1), 1);
            }
        }
    }
}
