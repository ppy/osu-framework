// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Layout.GridContainerTests
{
    [TestFixture]
    public class ValidationTest : LayoutTest
    {
        /// <summary>
        /// Tests that size is correctly validated when children update their size.
        /// </summary>
        [Test]
        public void TestChildUpdatesSize()
        {
            Box testBox;
            Box box;

            var grid = new GridContainer
            {
                Size = new Vector2(1000, 160),
                Content = new[]
                {
                    new Drawable[]
                    {
                        testBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Red
                        },
                        box = new Box
                        {
                            Size = new Vector2(150),
                            Colour = Color4.Green
                        }
                    }
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Distributed),
                    new Dimension(GridSizeMode.AutoSize),
                }
            };

            Run(grid, i =>
            {
                switch (i)
                {
                    case 0:
                        box.OnUpdate += performResize;
                        void performResize(Drawable drawable)
                        {
                            box.Width += 10;
                            box.OnUpdate = null;
                        }

                        return false;
                    case 1:
                        Assert.AreEqual(160, box.DrawWidth);
                        Assert.AreEqual(840, testBox.DrawWidth);
                        return true;
                    default:
                        return true;
                }
            });
        }
    }
}
