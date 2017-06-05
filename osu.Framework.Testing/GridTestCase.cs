// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using System.Linq;

namespace osu.Framework.Testing
{
    public abstract class GridTestCase : TestCase
    {
        private FillFlowContainer<Container> testContainer;

        protected int Rows { get; }
        protected int Cols { get; }

        public GridTestCase(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
        }

        private Container createCell() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Size = new Vector2(1.0f / Cols, 1.0f / Rows),
        };

        public override void Reset()
        {
            base.Reset();

            testContainer = new FillFlowContainer<Container> { RelativeSizeAxes = Axes.Both };
            for (int i = 0; i < Rows * Cols; ++i)
                testContainer.Add(createCell());

            Add(testContainer);
        }

        protected Container Cell(int index) => testContainer.Children.ElementAt(index);

        protected Container Cell(int row, int col) => Cell(col + row * Cols);
    }
}
