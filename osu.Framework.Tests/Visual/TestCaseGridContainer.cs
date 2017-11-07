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
                Content = new Drawable[][]
                {
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() },
                    new Drawable[] { new FillBox(), new FillBox(), new FillBox() }
                },
                Definition = new GridDefinition
                {
                    Rows = new[]
                    {
                        new RowDefinition(1) { Height = 50 }
                    },
                    Columns = new[]
                    {
                        new ColumnDefinition(1) { Width = 50 }
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
