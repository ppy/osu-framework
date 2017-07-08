// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Testing
{
    /// <summary>
    /// An abstract test case which exposes small cells arranged in a grid.
    /// Useful for displaying multiple configurations of a tested component at a glance.
    /// </summary>
    public abstract class GridTestCase : TestCase
    {
        private readonly FillFlowContainer<Container> testContainer;

        /// <summary>
        /// The amount of rows of the grid.
        /// </summary>
        protected int Rows { get; }

        /// <summary>
        /// The amount of columns of the grid.
        /// </summary>
        protected int Cols { get; }

        /// <summary>
        /// Constructs a grid test case with the given dimensions.
        /// </summary>
        /// <param name="rows">The amount of rows of the grid.</param>
        /// <param name="cols">The amount of columns of the grid.</param>
        protected GridTestCase(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;

            testContainer = new FillFlowContainer<Container> { RelativeSizeAxes = Axes.Both };
            for (int i = 0; i < Rows * Cols; ++i)
                testContainer.Add(createCell());

            Add(testContainer);
        }

        private Container createCell() => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Size = new Vector2(1.0f / Cols, 1.0f / Rows),
        };

        /// <summary>
        /// Access a cell by its index. Valid indices range from 0 to <see cref="Rows"/> * <see cref="Cols"/> - 1.
        /// </summary>
        protected Container Cell(int index) => testContainer.Children[index];

        /// <summary>
        /// Access a cell by its row and column.
        /// </summary>
        protected Container Cell(int row, int col) => Cell(col + row * Cols);
    }
}
