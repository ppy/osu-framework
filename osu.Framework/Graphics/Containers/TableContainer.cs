// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    public class TableContainer : CompositeDrawable
    {
        private readonly GridContainer grid;

        public TableContainer()
        {
            InternalChild = grid = new GridContainer { RelativeSizeAxes = Axes.Both };
        }

        private Drawable[,] content;

        /// <summary>
        /// The content of this <see cref="TableContainer"/>, arranged in a 2D rectangular array.
        /// <para>
        /// Null elements are allowed to represent blank rows/cells.
        /// </para>
        /// </summary>
        [CanBeNull]
        public Drawable[,] Content
        {
            get => content;
            set
            {
                if (content == value)
                    return;

                content = value;

                updateContent();
            }
        }

        private Column[] columns = Array.Empty<Column>();

        /// <summary>
        /// Describes the columns of this <see cref="TableContainer"/>.
        /// Each index of this array applies to the respective column index inside <see cref="Content"/>.
        /// </summary>
        [CanBeNull]
        public Column[] Columns
        {
            get => columns;
            set
            {
                value = value ?? Array.Empty<Column>();

                if (columns == value)
                    return;

                columns = value;

                updateContent();
            }
        }

        private Dimension rowDimension;

        /// <summary>
        /// Explicit dimensions for rows. The dimension is applied to every row of this <see cref="TableContainer"/>
        /// </summary>
        [CanBeNull]
        public Dimension RowDimension
        {
            get => rowDimension;
            set
            {
                if (rowDimension == value)
                    return;

                rowDimension = value;

                updateContent();
            }
        }

        private bool showHeaders = true;

        /// <summary>
        /// Whether to display a row with column headers at the top of the table.
        /// </summary>
        public bool ShowHeaders
        {
            get => showHeaders;
            set
            {
                if (showHeaders == value)
                    return;

                showHeaders = value;

                updateContent();
            }
        }

        public override Axes RelativeSizeAxes
        {
            get => base.RelativeSizeAxes;
            set
            {
                base.RelativeSizeAxes = value;
                updateGridSize();
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
            set
            {
                base.AutoSizeAxes = value;
                updateGridSize();
            }
        }

        public override void InvalidateFromChild(Invalidation invalidation, Drawable source = null)
        {
            base.InvalidateFromChild(invalidation, source);

            if ((invalidation & Invalidation.MiscGeometry) > 0)
                updateAnchors();
        }

        private int totalRows => content.GetLength(0) + (ShowHeaders ? 1 : 0);
        private int totalColumns => content.GetLength(1);

        private void updateContent()
        {
            if (content == null)
                grid.Content = null;
            else
            {
                grid.Content = getContentWithHeaders().ToJagged();

                grid.ColumnDimensions = columns.Select(c => c.Dimension).ToArray();
                grid.RowDimensions = Enumerable.Repeat(rowDimension ?? new Dimension(), totalRows).ToArray();
            }

            updateAnchors();
        }

        /// <summary>
        /// Adds headers, if required, and returns the resulting content. <see cref="content"/> is not modified in the process.
        /// </summary>
        /// <returns>The content, with headers added if required.</returns>
        private Drawable[,] getContentWithHeaders()
        {
            if (!ShowHeaders)
                return content;

            int rows = totalRows;
            int cols = totalColumns;

            var result = new Drawable[rows, cols];

            for (int row = 0; row < rows; row++)
            for (int col = 0; col < cols; col++)
            {
                if (row == 0)
                    result[row, col] = CreateHeader(col, Columns?[col]);
                else
                    result[row, col] = content[row - 1, col];
            }

            return result;
        }

        /// <summary>
        /// Ensures that all cells have the correct anchors defined by <see cref="Columns"/>.
        /// </summary>
        private void updateAnchors()
        {
            if (grid.Content == null)
                return;

            int rows = totalRows;
            int cols = totalColumns;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (col >= columns.Length)
                        break;

                    Drawable child = grid.Content[row][col];

                    if (child == null)
                        continue;

                    child.Origin = columns[col].Anchor;
                    child.Anchor = columns[col].Anchor;
                }
            }
        }

        /// <summary>
        /// Keeps the grid autosized in our autosized axes, and relative-sized in our non-autosized axes.
        /// </summary>
        private void updateGridSize()
        {
            grid.RelativeSizeAxes = Axes.None;
            grid.AutoSizeAxes = Axes.None;

            if ((AutoSizeAxes & Axes.X) == 0)
                grid.RelativeSizeAxes |= Axes.X;
            else
                grid.AutoSizeAxes |= Axes.X;

            if ((AutoSizeAxes & Axes.Y) == 0)
                grid.RelativeSizeAxes |= Axes.Y;
            else
                grid.AutoSizeAxes |= Axes.Y;
        }

        protected virtual Drawable CreateHeader(int index, [CanBeNull] Column column) => new SpriteText { Text = column?.Header ?? string.Empty };
    }

    public class Column
    {
        public readonly string Header;

        public readonly Anchor Anchor;

        public readonly Dimension Dimension;

        public Column(string header = null, Anchor anchor = Anchor.TopLeft, Dimension dimension = null)
        {
            Header = header ?? "";
            Anchor = anchor;
            Dimension = dimension ?? new Dimension();
        }
    }
}
