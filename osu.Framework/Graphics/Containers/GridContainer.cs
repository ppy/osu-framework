// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using osu.Framework.Caching;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.Containers
{
    public class GridContainer : CompositeDrawable
    {
        private IReadOnlyList<Dimension> rowDimensions;
        public IReadOnlyList<Dimension> RowDimensions
        {
            get { return rowDimensions; }
            set
            {
                if (rowDimensions == value)
                    return;
                rowDimensions = value;

                cellLayout.Invalidate();
            }
        }

        private IReadOnlyList<Dimension> columnDimensions;
        public IReadOnlyList<Dimension> ColumnDimensions
        {
            get { return columnDimensions; }
            set
            {
                if (columnDimensions == value)
                    return;
                columnDimensions = value;

                cellLayout.Invalidate();
            }
        }

        private Drawable[][] content;
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

        private int totalColumns => Content.Max(c => c.Length);
        private int totalRows => Content.Length;

        /// <summary>
        /// Moves content from <see cref="Content"/> into cells.
        /// </summary>
        private void layoutContent()
        {
            if (cellContent.IsValid)
                return;

            // Clear cell containers without disposing, as the content might be reused
            foreach (var cell in cells)
                cell.Clear(false);

            // It's easier to just re-construct the cell containers instead of resizing
            // If this becomes a bottleneck we can transition to using lists...
            ClearInternal();

            // Create the new cell containers
            cells = new CellContainer[totalRows, totalColumns];
            for (int r = 0; r < cells.GetLength(0); r++)
                for (int c = 0; c < cells.GetLength(1); c++)
                    AddInternal(cells[r, c] = new CellContainer());

            // Transfer content into the new cell containers
            for (int r = 0; r < Content.Length; r++)
                for (int c = 0; c < Content[r].Length; c++)
                    cells[r, c].Add(Content[r][c]);

            cellLayout.Invalidate();
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

            float definedWidth = 0;
            float definedHeight = 0;

            // Compute the width of explicitly-defined columns
            if (ColumnDimensions?.Count > 0)
            {
                foreach (var d in ColumnDimensions)
                {
                    for (int r = 0; r < cells.GetLength(0); r++)
                    {
                        cells[r, d.Index].IsWidthDefined = true;

                        if (d.Relative)
                            cells[r, d.Index].Width = d.Size * DrawWidth;
                        else
                            cells[r, d.Index].Width = d.Size;

                        definedWidth += cells[r, d.Index].Width;
                    }
                }
            }

            // Compute the height of explicitly-defined rows
            if (RowDimensions?.Count > 0)
            {
                foreach (var d in RowDimensions)
                {
                    for (int c = 0; c < cells.GetLength(1); c++)
                    {
                        cells[d.Index, c].IsHeightDefined = true;

                        if (d.Relative)
                            cells[d.Index, c].Height = d.Size * DrawHeight;
                        else
                            cells[d.Index, c].Height = d.Size;

                        definedHeight += cells[d.Index, c].Height;
                    }
                }
            }

            // Compute the dimensions for non-explicitly-defined columns/rows
            var autoSize = new Vector2
            (
                (DrawWidth - definedWidth) / totalColumns,
                (DrawHeight - definedHeight) / totalRows
            );

            // Add dimensions to non-explicitly-defined columns and add positional offsets
            for (int r = 0; r < cells.GetLength(0); r++)
                for (int c = 0; c < cells.GetLength(1); c++)
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

        private class CellContainer : Container
        {
            public bool IsWidthDefined;
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
            Index = index;
            Size = 0;
            Relative = false;
        }
    }
}
