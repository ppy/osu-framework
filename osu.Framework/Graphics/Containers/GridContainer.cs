// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using JetBrains.Annotations;
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
            get => content;
            set
            {
                if (content == value)
                    return;
                content = value;

                PropagateInvalidation(InvalidateCellContent());
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

                PropagateInvalidation(InvalidateCellLayout());
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

                PropagateInvalidation(InvalidateCellLayout());
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            validateCellContent();
        }

        protected override void Update()
        {
            base.Update();

            validateCellContent();
            validateCellLayout();
        }

        protected override void PropagateInvalidation(Invalidation invalidation)
        {
            if ((invalidation & Invalidation.ChildSizeBeforeAutoSize) != 0)
                invalidation |= InvalidateCellLayout();

            base.PropagateInvalidation(invalidation);
        }

        [MustUseReturnValue]
        protected Invalidation InvalidateCellLayout() => !cellLayout.Invalidate() ? 0 : InvalidateDrawSize() | InvalidateRequiredParentSizeToFit() | InvalidateBoundingBoxSizeBeforeParentAutoSize();

        [MustUseReturnValue]
        protected Invalidation InvalidateCellContent() => !cellContent.Invalidate() ? 0 : InvalidateCellLayout();

        private Cached cellContent = new Cached { Name = "GridContainer.cellContent" };
        private Cached cellLayout = new Cached { Name = "GridContainer.cellLayout" };

        private CellContainer[,] cells = new CellContainer[0, 0];
        private int cellRows => cells.GetLength(0);
        private int cellColumns => cells.GetLength(1);

        private void validateCellContent() => cellContent.ValidateWith(computeCellContent);

        private void validateCellLayout() => cellLayout.ValidateWith(computeCellLayout);

        /// <summary>
        /// Moves content from <see cref="Content"/> into cells.
        /// </summary>
        private void computeCellContent()
        {
            int requiredRows = Content?.Length ?? 0;
            int requiredColumns = requiredRows == 0 ? 0 : Content.Max(c => c?.Length ?? 0);

            // Clear cell containers without disposing, as the content might be reused
            foreach (var cell in cells)
                cell.Clear(false);

            // It's easier to just re-construct the cell containers instead of resizing
            // If this becomes a bottleneck we can transition to using lists, but this keeps the structure clean...
            ClearInternal();

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
        }

        /// <summary>
        /// Repositions/resizes cells.
        /// </summary>
        private void computeCellLayout()
        {
            validateCellContent();

            foreach (var cell in cells)
            {
                cell.DistributedWidth = true;
                cell.DistributedHeight = true;
                cell.IsAutoSized = false;
            }

            var childSize = ChildSizeBeforeAutoSize;

            int autoSizedRows = cellRows;
            int autoSizedColumns = cellColumns;

            float definedWidth = 0;
            float definedHeight = 0;

            // Compute the width of non-distributed columns
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
                            cellWidth = d.Size * childSize.X;
                            break;
                        case GridSizeMode.Absolute:
                            cellWidth = d.Size;
                            break;
                        case GridSizeMode.AutoSize:
                            for (int r = 0; r < cellRows; r++)
                            {
                                cells[r, i].IsAutoSized = true;
                                cellWidth = Math.Max(cellWidth, Content[r]?[i]?.BoundingBoxBeforeParentAutoSize.Width ?? 0);
                            }
                            break;
                    }

                    for (int r = 0; r < cellRows; r++)
                    {
                        cells[r, i].Width = cellWidth;
                        cells[r, i].DistributedWidth = false;
                    }

                    definedWidth += cellWidth;
                    autoSizedColumns--;
                }
            }

            // Compute the height of non-distributed rows
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
                            cellHeight = d.Size * childSize.Y;
                            break;
                        case GridSizeMode.Absolute:
                            cellHeight = d.Size;
                            break;
                        case GridSizeMode.AutoSize:
                            for (int c = 0; c < cellColumns; c++)
                            {
                                cells[i, c].IsAutoSized = true;
                                cellHeight = Math.Max(cellHeight, Content[i]?[c]?.BoundingBoxBeforeParentAutoSize.Height ?? 0);
                            }
                            break;
                    }

                    for (int c = 0; c < cellColumns; c++)
                    {
                        cells[i, c].Height = cellHeight;
                        cells[i, c].DistributedHeight = false;
                    }

                    definedHeight += cellHeight;
                    autoSizedRows--;
                }
            }

            // Compute the size which all distributed columns/rows should take on
            var distributedSize = new Vector2
            (
                Math.Max(0, childSize.X - definedWidth) / autoSizedColumns,
                Math.Max(0, childSize.Y - definedHeight) / autoSizedRows
            );

            // Add size to distributed columns/rows and add adjust cell positions
            for (int r = 0; r < cellRows; r++)
            for (int c = 0; c < cellColumns; c++)
            {
                if (cells[r, c].DistributedWidth)
                    cells[r, c].Width = distributedSize.X;
                if (cells[r, c].DistributedHeight)
                    cells[r, c].Height = distributedSize.Y;

                if (c > 0)
                    cells[r, c].X = cells[r, c - 1].X + cells[r, c - 1].Width;
                if (r > 0)
                    cells[r, c].Y = cells[r - 1, c].Y + cells[r - 1, c].Height;
            }
        }

        /// <summary>
        /// Represents one cell of the <see cref="GridContainer"/>.
        /// </summary>
        private class CellContainer : Container
        {
            /// <summary>
            /// Whether this <see cref="CellContainer"/> uses <see cref="GridSizeMode.Distributed"/> for its width.
            /// </summary>
            public bool DistributedWidth;

            /// <summary>
            /// Whether this <see cref="CellContainer"/> uses <see cref="GridSizeMode.Distributed"/> for its height.
            /// </summary>
            public bool DistributedHeight;

            /// <summary>
            /// Whether this <see cref="CellContainer"/> uses <see cref="GridSizeMode.AutoSize"/> for its width or height.
            /// </summary>
            public bool IsAutoSized;

            public override void InvalidateFromChild(Invalidation childInvalidation, Drawable child, Invalidation selfInvalidation = Invalidation.None)
            {
                if (IsAutoSized && (childInvalidation & Invalidation.BoundingBoxSizeBeforeParentAutoSize) != 0)
                {
                    if (Parent is GridContainer p)
                    {
                        p.PropagateInvalidation(p.InvalidateCellLayout());
                    }
                }

                base.InvalidateFromChild(childInvalidation, child, selfInvalidation);
            }
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
