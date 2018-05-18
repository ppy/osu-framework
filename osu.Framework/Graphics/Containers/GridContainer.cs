// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Allocation;
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
        /// Explicit dimensions for rows. Each index of this array applies to the respective row index inside <see cref="Content"/>.
        /// </summary>
        public Dimension[] RowDimensions
        {
            set
            {
                if (rowDimensions == value)
                    return;
                rowDimensions = value;

                cellLayout.Invalidate();
            }
        }

        private Dimension[] columnDimensions;
        /// <summary>
        /// Explicit dimensions for columns. Each index of this array applies to the respective column index inside <see cref="Content"/>.
        /// </summary>
        public Dimension[] ColumnDimensions
        {
            set
            {
                if (columnDimensions == value)
                    return;
                columnDimensions = value;

                cellLayout.Invalidate();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            layoutContent();
        }

        protected override void Update()
        {
            base.Update();

            layoutContent();
            layoutCells();
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & (Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit)) > 0)
                cellLayout.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        public override void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
        {
            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                cellLayout.Invalidate();

            base.InvalidateFromChild(invalidation, source);
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
                    cells[r, c] = new CellContainer();

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
                    cells[r, c].Depth = Content[r][c].Depth;

                    AddInternal(cells[r, c]);
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
                for (int i = 0; i < columnDimensions.Length; i++)
                {
                    if (i >= cellColumns)
                        continue;

                    var d = columnDimensions[i];

                    float cellWidth = 0;
                    switch (d.Mode)
                    {
                        case GridSizeMode.Distributed:
                            continue;
                        case GridSizeMode.Relative:
                            cellWidth = d.Size * DrawWidth;
                            break;
                        case GridSizeMode.Absolute:
                            cellWidth = d.Size;
                            break;
                        case GridSizeMode.AutoSize:
                            for (int r = 0; r < cellRows; r++)
                                cellWidth = Math.Max(cellWidth, Content[r]?[i]?.DrawWidth ?? 0);
                            break;
                    }

                    for (int r = 0; r < cellRows; r++)
                    {
                        cells[r, i].Width = cellWidth;
                        cells[r, i].IsWidthDefined = true;
                    }

                    definedWidth += cellWidth;
                    autoSizedColumns--;
                }
            }

            // Compute the height of explicitly-defined rows
            if (rowDimensions?.Length > 0)
            {
                for (int i = 0; i < rowDimensions.Length; i++)
                {
                    if (i >= cellRows)
                        continue;

                    var d = rowDimensions[i];

                    float cellHeight = 0;
                    switch (d.Mode)
                    {
                        case GridSizeMode.Distributed:
                            continue;
                        case GridSizeMode.Relative:
                            cellHeight = d.Size * DrawHeight;
                            break;
                        case GridSizeMode.Absolute:
                            cellHeight = d.Size;
                            break;
                        case GridSizeMode.AutoSize:
                            for (int c = 0; c < cellColumns; c++)
                                cellHeight = Math.Max(cellHeight, Content[i]?[c]?.DrawHeight ?? 0);
                            break;
                    }

                    for (int c = 0; c < cellColumns; c++)
                    {
                        cells[i, c].IsHeightDefined = true;
                        cells[i, c].Height = cellHeight;
                    }

                    definedHeight += cellHeight;
                    autoSizedRows--;
                }
            }

            // Compute the size of non-explicitly defined rows/columns that should fill the remaining area
            var autoSize = new Vector2
            (
                Math.Max(0, DrawWidth - definedWidth) / autoSizedColumns,
                Math.Max(0, DrawHeight - definedHeight) / autoSizedRows
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
        /// The mode in which this row or column <see cref="GridContainer"/> is sized.
        /// </summary>
        public GridSizeMode Mode { get; private set; }

        /// <summary>
        /// The size of the row or column which this <see cref="Dimension"/> applies to.
        /// </summary>
        public float Size { get; private set; }

        /// <summary>
        /// Constructs a new <see cref="Dimension"/>.
        /// </summary>
        /// <param name="mode">The sizing mode to use.</param>
        /// <param name="size">The size of this row or column. This only has an effect if <paramref name="mode"/> is not <see cref="GridSizeMode.Distributed"/>.</param>
        public Dimension(GridSizeMode mode = GridSizeMode.Distributed, float size = 0)
        {
            Mode = mode;
            Size = size;
        }
    }

    public enum GridSizeMode
    {
        /// <summary>
        /// Any remaining area of the <see cref="GridContainer"/> will be divided amongst this and all
        /// other elements which use <see cref="GridSizeMode.Distributed"/>.
        /// </summary>
        Distributed,
        /// <summary>
        /// This element should be sized relative to the dimensions of the <see cref="GridContainer"/>.
        /// </summary>
        Relative,
        /// <summary>
        /// This element has a size independent of the <see cref="GridContainer"/>.
        /// </summary>
        Absolute,
        /// <summary>
        /// This element will be sized to the maximum size along its span.
        /// </summary>
        AutoSize
    }
}
