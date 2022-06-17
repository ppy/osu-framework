// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Layout;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which allows laying out <see cref="Drawable"/>s in a grid.
    /// </summary>
    public class GridContainer : CompositeDrawable
    {
        public GridContainer()
        {
            AddLayout(cellLayout);
            AddLayout(cellChildLayout);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            layoutContent();
        }

        private GridContainerContent content;

        /// <summary>
        /// The content of this <see cref="GridContainer"/>, arranged in a 2D grid array, where each array
        /// of <see cref="Drawable"/>s represents a row and each element of that array represents a column.
        /// <para>
        /// Null elements are allowed to represent blank rows/cells.
        /// </para>
        /// </summary>
        public GridContainerContent Content
        {
            get => content;
            set
            {
                if (content?.Equals(value) == true)
                    return;

                if (content != null)
                    content.ArrayElementChanged -= onContentChange;

                content = value;

                onContentChange();

                if (content != null)
                    content.ArrayElementChanged += onContentChange;
            }
        }

        private void onContentChange()
        {
            cellContent.Invalidate();
        }

        private Dimension[] rowDimensions = Array.Empty<Dimension>();

        /// <summary>
        /// Explicit dimensions for rows. Each index of this array applies to the respective row index inside <see cref="Content"/>.
        /// </summary>
        public Dimension[] RowDimensions
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (rowDimensions == value)
                    return;

                rowDimensions = value;

                cellLayout.Invalidate();
            }
        }

        private Dimension[] columnDimensions = Array.Empty<Dimension>();

        /// <summary>
        /// Explicit dimensions for columns. Each index of this array applies to the respective column index inside <see cref="Content"/>.
        /// </summary>
        public Dimension[] ColumnDimensions
        {
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (columnDimensions == value)
                    return;

                columnDimensions = value;

                cellLayout.Invalidate();
            }
        }

        /// <summary>
        /// Controls which <see cref="Axes"/> are automatically sized w.r.t. <see cref="CompositeDrawable.InternalChildren"/>.
        /// Children's <see cref="Drawable.BypassAutoSizeAxes"/> are ignored for automatic sizing.
        /// Most notably, <see cref="Drawable.RelativePositionAxes"/> and <see cref="Drawable.RelativeSizeAxes"/> of children
        /// do not affect automatic sizing to avoid circular size dependencies.
        /// It is not allowed to manually set <see cref="Drawable.Size"/> (or <see cref="Drawable.Width"/> / <see cref="Drawable.Height"/>)
        /// on any <see cref="Axes"/> which are automatically sized.
        /// </summary>
        public new Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set => base.AutoSizeAxes = value;
        }

        protected override void Update()
        {
            base.Update();

            layoutContent();
            layoutCells();
        }

        private readonly Cached cellContent = new Cached();
        private readonly LayoutValue cellLayout = new LayoutValue(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit);
        private readonly LayoutValue cellChildLayout = new LayoutValue(Invalidation.RequiredParentSizeToFit | Invalidation.Presence, InvalidationSource.Child);

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

            int requiredRows = Content?.Count ?? 0;
            int requiredColumns = requiredRows == 0 ? 0 : Content?.Max(c => c?.Count ?? 0) ?? 0;

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
            {
                for (int c = 0; c < cellColumns; c++)
                {
                    // Add cell
                    cells[r, c] = new CellContainer();

                    // Allow empty rows
                    if (Content[r] == null)
                        continue;

                    // Allow non-square grids
                    if (c >= Content[r].Count)
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

            cellContent.Validate();
        }

        /// <summary>
        /// Repositions/resizes cells.
        /// </summary>
        private void layoutCells()
        {
            if (!cellChildLayout.IsValid)
            {
                cellLayout.Invalidate();
                cellChildLayout.Validate();
            }

            if (cellLayout.IsValid)
                return;

            float[] widths = distribute(columnDimensions, DrawWidth, getCellSizesAlongAxis(Axes.X, DrawWidth));
            float[] heights = distribute(rowDimensions, DrawHeight, getCellSizesAlongAxis(Axes.Y, DrawHeight));

            for (int col = 0; col < cellColumns; col++)
            {
                for (int row = 0; row < cellRows; row++)
                {
                    cells[row, col].Size = new Vector2(widths[col], heights[row]);

                    if (col > 0)
                        cells[row, col].X = cells[row, col - 1].X + cells[row, col - 1].Width;

                    if (row > 0)
                        cells[row, col].Y = cells[row - 1, col].Y + cells[row - 1, col].Height;
                }
            }

            cellLayout.Validate();
        }

        /// <summary>
        /// Retrieves the size of all cells along the span of an axis.
        /// For the X-axis, this retrieves the size of all columns.
        /// For the Y-axis, this retrieves the size of all rows.
        /// </summary>
        /// <param name="axis">The axis span.</param>
        /// <param name="spanLength">The absolute length of the span.</param>
        /// <returns>The size of all cells along the span of <paramref name="axis"/>.</returns>
        /// <exception cref="InvalidOperationException">If the <see cref="Dimension"/> for a cell is unsupported.</exception>
        private float[] getCellSizesAlongAxis(Axes axis, float spanLength)
        {
            var spanDimensions = axis == Axes.X ? columnDimensions : rowDimensions;
            int spanCount = axis == Axes.X ? cellColumns : cellRows;

            float[] sizes = new float[spanCount];

            for (int i = 0; i < spanCount; i++)
            {
                if (i >= spanDimensions.Length)
                    break;

                var dimension = spanDimensions[i];

                switch (dimension.Mode)
                {
                    default:
                        throw new InvalidOperationException($"Unsupported dimension: {dimension.Mode}.");

                    case GridSizeMode.Distributed:
                        break;

                    case GridSizeMode.Relative:
                        sizes[i] = dimension.Size * spanLength;
                        break;

                    case GridSizeMode.Absolute:
                        sizes[i] = dimension.Size;
                        break;

                    case GridSizeMode.AutoSize:
                        float size = 0;

                        if (axis == Axes.X)
                        {
                            // Go through each row and get the width of the cell at the indexed column
                            for (int r = 0; r < cellRows; r++)
                            {
                                var cell = Content[r]?[i];
                                if (cell == null || cell.RelativeSizeAxes.HasFlagFast(axis))
                                    continue;

                                size = Math.Max(size, getCellWidth(cell));
                            }
                        }
                        else
                        {
                            // Go through each column and get the height of the cell at the indexed row
                            for (int c = 0; c < cellColumns; c++)
                            {
                                var cell = Content[i]?[c];
                                if (cell == null || cell.RelativeSizeAxes.HasFlagFast(axis))
                                    continue;

                                size = Math.Max(size, getCellHeight(cell));
                            }
                        }

                        sizes[i] = size;
                        break;
                }

                sizes[i] = Math.Clamp(sizes[i], dimension.MinSize, dimension.MaxSize);
            }

            return sizes;
        }

        private static bool shouldConsiderCell(Drawable cell) => cell != null && cell.IsAlive && cell.IsPresent;
        private static float getCellWidth(Drawable cell) => shouldConsiderCell(cell) ? cell.BoundingBox.Width : 0;
        private static float getCellHeight(Drawable cell) => shouldConsiderCell(cell) ? cell.BoundingBox.Height : 0;

        /// <summary>
        /// Distributes any available length along all distributed dimensions, if required.
        /// </summary>
        /// <param name="dimensions">The full dimensions of the row or column.</param>
        /// <param name="spanLength">The total available length.</param>
        /// <param name="cellSizes">An array containing pre-filled sizes of any non-distributed cells. This array will be mutated.</param>
        /// <returns><paramref name="cellSizes"/>.</returns>
        private float[] distribute(Dimension[] dimensions, float spanLength, float[] cellSizes)
        {
            // Indices of all distributed cells
            int[] distributedIndices = Enumerable.Range(0, cellSizes.Length).Where(i => i >= dimensions.Length || dimensions[i].Mode == GridSizeMode.Distributed).ToArray();

            // The dimensions corresponding to all distributed cells
            IEnumerable<DimensionEntry> distributedDimensions = distributedIndices.Select(i => new DimensionEntry(i, i >= dimensions.Length ? new Dimension() : dimensions[i]));

            // Total number of distributed cells
            int distributionCount = distributedIndices.Length;

            // Non-distributed size
            float requiredSize = cellSizes.Sum();

            // Distribution size for _each_ distributed cell
            float distributionSize = Math.Max(0, spanLength - requiredSize) / distributionCount;

            // Write the sizes of distributed cells. Ordering is important to maximize excess at every step
            foreach (var entry in distributedDimensions.OrderBy(d => d.Dimension.Range))
            {
                // Cells start off at their minimum size, and the total size should not exceed their maximum size
                cellSizes[entry.Index] = Math.Min(entry.Dimension.MaxSize, entry.Dimension.MinSize + distributionSize);

                // If there's no excess, any further distributions are guaranteed to also have no excess, so this becomes a null-op
                // If there is an excess, the excess should be re-distributed among all other n-1 distributed cells
                if (--distributionCount > 0)
                    distributionSize += Math.Max(0, distributionSize - entry.Dimension.Range) / distributionCount;
            }

            return cellSizes;
        }

        private readonly struct DimensionEntry
        {
            public readonly int Index;
            public readonly Dimension Dimension;

            public DimensionEntry(int index, Dimension dimension)
            {
                Index = index;
                Dimension = dimension;
            }
        }

        /// <summary>
        /// Represents one cell of the <see cref="GridContainer"/>.
        /// </summary>
        private class CellContainer : Container
        {
            protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
            {
                bool result = base.OnInvalidate(invalidation, source);

                if (source == InvalidationSource.Child && (invalidation & (Invalidation.RequiredParentSizeToFit | Invalidation.Presence)) > 0)
                    result |= Parent?.Invalidate(invalidation, InvalidationSource.Child) ?? false;

                return result;
            }
        }
    }

    /// <summary>
    /// Defines the size of a row or column in a <see cref="GridContainer"/>.
    /// </summary>
    public class Dimension
    {
        /// <summary>
        /// The mode in which this row or column <see cref="GridContainer"/> is sized.
        /// </summary>
        public readonly GridSizeMode Mode;

        /// <summary>
        /// The size of the row or column which this <see cref="Dimension"/> applies to.
        /// Only has an effect if <see cref="Mode"/> is not <see cref="GridSizeMode.Distributed"/>.
        /// </summary>
        public readonly float Size;

        /// <summary>
        /// The minimum size of the row or column which this <see cref="Dimension"/> applies to.
        /// </summary>
        public readonly float MinSize;

        /// <summary>
        /// The maximum size of the row or column which this <see cref="Dimension"/> applies to.
        /// </summary>
        public readonly float MaxSize;

        /// <summary>
        /// Constructs a new <see cref="Dimension"/>.
        /// </summary>
        /// <param name="mode">The sizing mode to use.</param>
        /// <param name="size">The size of this row or column. This only has an effect if <paramref name="mode"/> is not <see cref="GridSizeMode.Distributed"/>.</param>
        /// <param name="minSize">The minimum size of this row or column.</param>
        /// <param name="maxSize">The maximum size of this row or column.</param>
        public Dimension(GridSizeMode mode = GridSizeMode.Distributed, float size = 0, float minSize = 0, float maxSize = float.MaxValue)
        {
            if (minSize < 0)
                throw new ArgumentOutOfRangeException(nameof(minSize), "Must be greater than 0.");

            if (minSize > maxSize)
                throw new ArgumentOutOfRangeException(nameof(minSize), $"Must be less than {nameof(maxSize)}.");

            Mode = mode;
            Size = size;
            MinSize = minSize;
            MaxSize = maxSize;
        }

        /// <summary>
        /// The range of the size of this <see cref="Dimension"/>.
        /// </summary>
        internal float Range => MaxSize - MinSize;
    }

    public enum GridSizeMode
    {
        /// <summary>
        /// Any remaining area of the <see cref="GridContainer"/> will be divided amongst this and all
        /// other elements which use <see cref="Distributed"/>.
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
