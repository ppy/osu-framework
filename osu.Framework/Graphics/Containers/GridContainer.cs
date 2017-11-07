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
        private GridDefinition definition;
        public GridDefinition Definition
        {
            get { return definition; }
            set
            {
                if (definition == value)
                    return;
                definition = value;

                cellLayout.Invalidate();
                cellContent.Invalidate();
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


        private Cached cellContent = new Cached();
        private Cached cellLayout = new Cached();

        private CellContainer[,] cells = new CellContainer[0, 0];

        private int totalColumns => Content.Max(c => c.Length);
        private int totalRows => Content.Length;

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
            if (Definition.Columns?.Count > 0)
            {
                foreach (var d in Definition.Columns)
                {
                    for (int r = 0; r < cells.GetLength(0); r++)
                    {
                        cells[r, d.Index].IsWidthDefined = true;

                        if (d.IsRelative)
                            cells[r, d.Index].Width = d.Width * DrawWidth;
                        else
                            cells[r, d.Index].Width = d.Width;

                        definedWidth += cells[r, d.Index].Width;
                    }
                }
            }

            // Compute the height of explicitly-defined rows
            if (Definition.Rows?.Count > 0)
            {
                foreach (var d in Definition.Rows)
                {
                    for (int c = 0; c < cells.GetLength(1); c++)
                    {
                        cells[d.Index, c].IsHeightDefined = true;

                        if (d.IsRelative)
                            cells[d.Index, c].Height = d.Height * DrawHeight;
                        else
                            cells[d.Index, c].Height = d.Height;

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
    /// Defines the characteristics of a row in a <see cref="GridContainer"/>.
    /// </summary>
    public class RowDefinition
    {
        /// <summary>
        /// The height of this row.
        /// </summary>
        public float Height;

        /// <summary>
        /// Whether <see cref="Height"/> is relative to the height of the <see cref="GridContainer"/>.
        /// </summary>
        public bool IsRelative;

        /// <summary>
        /// The index of the row which this definition affects.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Constructs a <see cref="RowDefinition"/>.
        /// </summary>
        /// <param name="rowIndex">The index of the row which this definition affects.</param>
        public RowDefinition(int rowIndex)
        {
            Index = rowIndex;
        }
    }

    /// <summary>
    /// Defines the characteristics of a column in a <see cref="GridContainer"/>.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// The width of this column.
        /// </summary>
        public float Width;

        /// <summary>
        /// Whether <see cref="Width"/> is relative to the width of the <see cref="GridContainer"/>.
        /// </summary>
        public bool IsRelative;

        /// <summary>
        /// The index of the column which this definition affects.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Constructs a <see cref="ColumnDefinition"/>.
        /// </summary>
        /// <param name="columnIndex">The index of the column which this definition affects.</param>
        public ColumnDefinition(int columnIndex)
        {
            Index = columnIndex;
        }
    }

    public class GridDefinition
    {
        public IReadOnlyList<RowDefinition> Rows { get; set; }
        public IReadOnlyList<ColumnDefinition> Columns { get; set; }
    }
}
