// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osuTK;

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

                cellContent.Invalidate();
            }
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
                    throw new ArgumentNullException(nameof(RowDimensions));

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
                    throw new ArgumentNullException(nameof(ColumnDimensions));

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
            if ((invalidation & (Invalidation.RequiredParentSizeToFit | Invalidation.Presence)) > 0)
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

            distribute(columnDimensions, Axes.X);
            distribute(rowDimensions, Axes.Y);

            cellLayout.Validate();
        }

        /// <summary>
        /// Distributes dimensions along an axis.
        /// </summary>
        /// <param name="dimensions">The dimensions to distribute.</param>
        /// <param name="axis">The axis to distribute the dimensions along.</param>
        private void distribute(Dimension[] dimensions, Axes axis)
        {
            int axisCells = axis == Axes.X ? cellColumns : cellRows;
            int oppositeAxisCells = axis == Axes.X ? cellRows : cellColumns;

            // Cells which require distribution
            Span<bool> requiresDistribution = stackalloc bool[axisCells];
            requiresDistribution.Fill(true);

            float definedSize = 0;
            int distributionCount = axisCells;

            // Compute the size of non-distributed cells
            for (int i = 0; i < dimensions.Length; i++)
            {
                if (i >= axisCells)
                    break;

                var d = dimensions[i];

                float cellSize = 0;
                switch (d.Mode)
                {
                    case GridSizeMode.Distributed:
                        continue;
                    case GridSizeMode.Relative:
                        cellSize = d.Size * getSizeAlongAxis(axis);
                        break;
                    case GridSizeMode.Absolute:
                        cellSize = d.Size;
                        break;
                    case GridSizeMode.AutoSize:
                        cellSize = getBoundingSizeAlongAxis(axis, i);
                        break;
                }

                cellSize = MathHelper.Clamp(cellSize, d.MinSize, d.MaxSize);

                switch (axis)
                {
                    case Axes.X:
                        for (int j = 0; j < oppositeAxisCells; j++)
                            cells[j, i].Width = cellSize;
                        break;
                    case Axes.Y:
                        for (int j = 0; j < oppositeAxisCells; j++)
                            cells[i, j].Height = cellSize;
                        break;
                }

                requiresDistribution[i] = false;

                definedSize += cellSize;
                distributionCount--;
            }

            // Compute the size which all distributed cells should take on
            float distributionSize = (getSizeAlongAxis(axis) - definedSize) / distributionCount;

            // Redistribute excess from distributed cells which would receive a larger size than they want
            if (distributionCount > 1)
            {
                // For each distributed column, add the excess back to the distribution pool
                // It's important to note that this
                foreach (var d in dimensions.Where(d => d.Mode == GridSizeMode.Distributed).OrderBy(d => -d.MinSize).ThenBy(d => d.MaxSize))
                {
                    if (distributionSize <= d.MaxSize && distributionSize >= d.MinSize)
                        continue;

                    // Remove this cell from the distribution pool
                    distributionCount--;

                    if (distributionCount == 0)
                        break;

                    float excess = 0;

                    if (distributionSize > d.MaxSize)
                        excess = distributionSize - d.MaxSize;
                    else if (distributionSize < d.MinSize)
                        excess = distributionSize - d.MinSize; // Negative excess

                    // Redistribute the excess between all other cells, each receiving a fraction of the excess
                    distributionSize += excess / distributionCount;
                }
            }

            // Add size to distributed columns/rows and add adjust cell positions
            for (int i = 0; i < axisCells; i++)
            for (int j = 0; j < oppositeAxisCells; j++)
            {
                float clampedDistributionSize = distributionSize;

                if (requiresDistribution[i])
                {
                    float minSize = i >= dimensions.Length ? float.MinValue : dimensions[i].MinSize;
                    float maxSize = i >= dimensions.Length ? float.MaxValue : dimensions[i].MaxSize;
                    clampedDistributionSize = MathHelper.Clamp(clampedDistributionSize, minSize, maxSize);
                }

                switch (axis)
                {
                    case Axes.X:
                        if (requiresDistribution[i])
                            cells[j, i].Width = clampedDistributionSize;

                        if (i > 0)
                            cells[j, i].X = cells[j, i - 1].X + cells[j, i - 1].Width;
                        break;
                    case Axes.Y:
                        if (requiresDistribution[i])
                            cells[i, j].Height = clampedDistributionSize;

                        if (i > 0)
                            cells[i, j].Y = cells[i - 1, j].Y + cells[i - 1, j].Height;
                        break;
                }
            }
        }

        /// <summary>
        /// Retrieves the size of this <see cref="GridContainer"/> along an axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <returns>This <see cref="GridContainer"/>'s <see cref="Drawable.DrawWidth"/> or <see cref="Drawable.DrawHeight"/>, depending on <paramref name="axis"/>.</returns>
        private float getSizeAlongAxis(Axes axis)
        {
            switch (axis)
            {
                case Axes.X:
                    return DrawWidth;
                case Axes.Y:
                    return DrawHeight;
                default:
                    throw new ArgumentException("Unsupported axis.", nameof(axis));
            }
        }

        /// <summary>
        /// Retrieves the minimum size required to bound children along an axis.
        /// </summary>
        /// <param name="axis">The axis of interest.</param>
        /// <param name="index">The row or column which the bound should be computed for. </param>
        /// <returns>The minimum size required along the <paramref name="axis"/> to bound all elements along the row or column at <paramref name="index"/> in the grid.
        /// E.g. for an index of 0 and an axis of X, this will return the minimum width required to bound all children within the first column.
        /// </returns>
        /// <exception cref="ArgumentException"></exception>
        private float getBoundingSizeAlongAxis(Axes axis, int index)
        {
            float size = 0;

            switch (axis)
            {
                case Axes.X:
                    for (int r = 0; r < cellRows; r++)
                        size = Math.Max(size, Content[r]?[index]?.BoundingBox.Width ?? 0);
                    break;
                case Axes.Y:
                    for (int c = 0; c < cellColumns; c++)
                        size = Math.Max(size, Content[index]?[c]?.BoundingBox.Height ?? 0);
                    break;
                default:
                    throw new ArgumentException("Unsupported axis.", nameof(axis));
            }

            return size;
        }

        /// <summary>
        /// Represents one cell of the <see cref="GridContainer"/>.
        /// </summary>
        private class CellContainer : Container
        {
            public override void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
            {
                if ((invalidation & (Invalidation.RequiredParentSizeToFit | Invalidation.Presence)) > 0)
                    Parent?.InvalidateFromChild(invalidation, this);

                base.InvalidateFromChild(invalidation, source);
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
        public Dimension(GridSizeMode mode = GridSizeMode.Distributed, float size = 0, float minSize = float.MinValue, float maxSize = float.MaxValue)
        {
            Mode = mode;
            Size = size;
            MinSize = minSize;
            MaxSize = maxSize;
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
