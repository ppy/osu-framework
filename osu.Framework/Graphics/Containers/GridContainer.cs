// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using OpenTK;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which allows laying out <see cref="Drawable"/>s in a grid.
    /// </summary>
    public class GridContainer : CompositeDrawable
    {
        private Drawable[][] content;
        /// <summary>
        /// The content of this <see cref="GridContainer"/>, arranged in a 2D grid array, where each array
        /// of <see cref="Drawable"/>s represents a row and each element of that array represents a column.
        /// <para>
        /// Null elements are allowed to represent blank rows/cells.
        /// </para>
        /// </summary>
        public Drawable[][] Content
        {
            get { return content; }
            set
            {
                if (content == value)
                    return;
                content = value;

                cellContent.Invalidate();
            }
        }

        private Dimension[] rowDimensions;
        /// <summary>
        /// Explicit dimensions for rows. This is only required for rows where absolute/relative sizing is desired.
        /// </summary>
        public Dimension[] RowDimensions
        {
            set
            {
                if (rowDimensions == value)
                    return;

                if (value.GroupBy(v => v.Index).Any(g => g.Count() > 1))
                    throw new ArgumentException($"Indices defined by {nameof(RowDimensions)} must be unique.", nameof(value));

                rowDimensions = value;
                cellLayout.Invalidate();
            }
        }

        private Dimension[] columnDimensions;
        /// <summary>
        /// Explicit dimensions for columns. This is only required for columns where absolute/relative sizing is desired.
        /// </summary>
        public Dimension[] ColumnDimensions
        {
            set
            {
                if (columnDimensions == value)
                    return;

                if (value.GroupBy(v => v.Index).Any(g => g.Count() > 1))
                    throw new ArgumentException($"Indices defined by {nameof(ColumnDimensions)} must be unique.", nameof(value));

                columnDimensions = value;
                cellLayout.Invalidate();
            }
        }

        protected override void Update()
        {
            base.Update();

            layoutContent();
            layoutCells();
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.DrawSize) > 0)
                cellLayout.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        private Cached cellContent = new Cached();
        private Cached cellLayout = new Cached();

        private CellContainer[,] cells = new CellContainer[0, 0];
        private int cellRows => cells.GetLength(0);
        private int cellColumns => cells.GetLength(1);

        /// <summary>
        /// Moves content from <see cref="Content"/> into cells.
        /// </summary>
        private void layoutContent()
        {
            if (cellContent.IsValid)
                return;

            int requiredRows = Content?.Length ?? 0;
            int requiredColumns = requiredRows == 0 ? 0 : Content.Max(c => c?.Length ?? 0);

            // Clear cell containers without disposing, as the content might be reused
            foreach (var cell in cells)
                cell.Clear(false);

            // It's easier to just re-construct the cell containers instead of resizing
            // If this becomes a bottleneck we can transition to using lists, but this keeps the structure clean...
            ClearInternal();
            cellLayout.Invalidate();

            // Create the new cell containers and add content
            cells = new CellContainer[requiredRows, requiredColumns];
            for (int r = 0; r < cellRows; r++)
                for (int c = 0; c < cellColumns; c++)
                {
                    // Add cell
                    AddInternal(cells[r, c] = new CellContainer());

                    // Allow empty rows
                    if (Content[r] == null)
                        continue;

                    // Allow non-square grids
                    if (c >= Content[r].Length)
                        continue;

                    // Allow empty cells
                    if (Content[r][c] == null)
                        continue;

                    // Add content
                    cells[r, c].Add(Content[r][c]);
                }

            cellContent.Validate();
        }

        /// <summary>
        /// Repositions/resizes cells.
        /// </summary>
        private void layoutCells()
        {
            if (cellLayout.IsValid)
                return;

            foreach (var cell in cells)
            {
                cell.IsWidthDefined = false;
                cell.IsHeightDefined = false;
            }

            int autoSizedRows = cellRows;
            int autoSizedColumns = cellColumns;

            float definedWidth = 0;
            float definedHeight = 0;

            // Compute the width of explicitly-defined columns
            if (columnDimensions?.Length > 0)
            {
                foreach (var d in columnDimensions)
                {
                    if (d.Index >= cellColumns)
                        continue;

                    for (int r = 0; r < cellRows; r++)
                    {
                        cells[r, d.Index].IsWidthDefined = true;

                        if (d.Relative)
                            cells[r, d.Index].Width = d.Size * DrawWidth;
                        else
                            cells[r, d.Index].Width = d.Size;
                    }

                    definedWidth += d.Size;
                    autoSizedColumns--;
                }
            }

            // Compute the height of explicitly-defined rows
            if (rowDimensions?.Length > 0)
            {
                foreach (var d in rowDimensions)
                {
                    if (d.Index >= cellRows)
                        continue;

                    for (int c = 0; c < cellColumns; c++)
                    {
                        cells[d.Index, c].IsHeightDefined = true;

                        if (d.Relative)
                            cells[d.Index, c].Height = d.Size * DrawHeight;
                        else
                            cells[d.Index, c].Height = d.Size;
                    }

                    definedHeight += d.Size;
                    autoSizedRows--;
                }
            }

            // Compute the size of non-explicitly defined rows/columns that should fill the remaining area
            var autoSize = new Vector2
            (
                (DrawWidth - definedWidth) / autoSizedColumns,
                (DrawHeight - definedHeight) / autoSizedRows
            );

            // Add sizing to non-explicitly-defined columns and add positional offsets
            for (int r = 0; r < cellRows; r++)
                for (int c = 0; c < cellColumns; c++)
                {
                    if (!cells[r, c].IsWidthDefined)
                        cells[r, c].Width = autoSize.X;
                    if (!cells[r, c].IsHeightDefined)
                        cells[r, c].Height = autoSize.Y;

                    if (c > 0)
                        cells[r, c].X = cells[r, c - 1].X + cells[r, c - 1].Width;
                    if (r > 0)
                        cells[r, c].Y = cells[r - 1, c].Y + cells[r - 1, c].Height;
                }

            cellLayout.Validate();
        }

        /// <summary>
        /// Represents one cell of the <see cref="GridContainer"/>.
        /// </summary>
        private class CellContainer : Container
        {
            /// <summary>
            /// Whether this <see cref="CellContainer"/> has an explicitly-defined width.
            /// </summary>
            public bool IsWidthDefined;

            /// <summary>
            /// Whether this <see cref="CellContainer"/> has an explicitly-defined height.
            /// </summary>
            public bool IsHeightDefined;
        }
    }

    /// <summary>
    /// Defines the size of a row or column in a <see cref="GridContainer"/>.
    /// </summary>
    public struct Dimension
    {
        /// <summary>
        /// The index of the row or column in the <see cref="GridContainer"/> which this <see cref="Dimension"/> applies to.
        /// </summary>
        public int Index;

        /// <summary>
        /// The size of the row or column which this <see cref="Dimension"/> applies to.
        /// </summary>
        public float Size;

        /// <summary>
        /// Whether <see cref="Size"/> is relative to <see cref="GridContainer.Size"/>.
        /// </summary>
        public bool Relative;

        /// <summary>
        /// Constructs a new <see cref="Dimension"/>.
        /// </summary>
        /// <param name="index">The index of the row or column in the <see cref="GridContainer"/> which this <see cref="Dimension"/> applies to.</param>
        public Dimension(int index)
        {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));

            Index = index;
            Size = 0;
            Relative = false;
        }
    }
}
