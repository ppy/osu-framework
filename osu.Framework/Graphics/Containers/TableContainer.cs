// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which tabulates <see cref="Drawable"/>s.
    /// </summary>
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

        private TableColumn[] columns = Array.Empty<TableColumn>();

        /// <summary>
        /// Describes the columns of this <see cref="TableContainer"/>.
        /// Each index of this array applies to the respective column index inside <see cref="Content"/>.
        /// </summary>
        public TableColumn[] Columns
        {
            [NotNull]
            get => columns;
            [CanBeNull]
            set
            {
                value ??= Array.Empty<TableColumn>();

                if (columns == value)
                    return;

                columns = value;

                updateContent();
            }
        }

        private Dimension rowSize = new Dimension();

        /// <summary>
        /// Explicit dimensions for rows. The dimension is applied to every row of this <see cref="TableContainer"/>
        /// </summary>
        public Dimension RowSize
        {
            [NotNull]
            get => rowSize;
            [CanBeNull]
            set
            {
                value ??= new Dimension();

                if (rowSize == value)
                    return;

                rowSize = value;

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

        /// <summary>
        /// The total number of rows in the content, including the header.
        /// </summary>
        private int totalRows => (content?.GetLength(0) ?? 0) + (ShowHeaders ? 1 : 0);

        /// <summary>
        /// The total number of columns in the content, including the header.
        /// </summary>
        private int totalColumns
        {
            get
            {
                if (columns == null || !showHeaders)
                    return content?.GetLength(1) ?? 0;

                return Math.Max(columns.Length, content?.GetLength(1) ?? 0);
            }
        }

        /// <summary>
        /// Adds content to the underlying grid.
        /// </summary>
        private void updateContent()
        {
            grid.Content = getContentWithHeaders().ToJagged();

            grid.ColumnDimensions = columns.Select(c => c.Dimension).ToArray();
            grid.RowDimensions = Enumerable.Repeat(RowSize, totalRows).ToArray();

            updateAnchors();
        }

        /// <summary>
        /// Adds headers, if required, and returns the resulting content. <see cref="content"/> is not modified in the process.
        /// </summary>
        /// <returns>The content, with headers added if required.</returns>
        private Drawable[,] getContentWithHeaders()
        {
            if (!ShowHeaders || Columns == null || Columns.Length == 0)
                return content;

            int rowCount = totalRows;
            int columnCount = totalColumns;

            var result = new Drawable[rowCount, columnCount];

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
                {
                    if (row == 0)
                        result[row, col] = CreateHeader(col, col >= Columns?.Length ? null : Columns?[col]);
                    else if (col < content.GetLength(1))
                        result[row, col] = content[row - 1, col];
                }
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

            int rowCount = totalRows;
            int columnCount = totalColumns;

            for (int row = 0; row < rowCount; row++)
            {
                for (int col = 0; col < columnCount; col++)
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

        /// <summary>
        /// Creates the content for a cell in the header row of the table.
        /// </summary>
        /// <param name="index">The column index.</param>
        /// <param name="column">The column definition.</param>
        /// <returns>The cell content.</returns>
        protected virtual Drawable CreateHeader(int index, [CanBeNull] TableColumn column) => new SpriteText { Text = column?.Header ?? string.Empty };
    }

    /// <summary>
    /// Defines a column of the <see cref="TableContainer"/>.
    /// </summary>
    public class TableColumn
    {
        /// <summary>
        /// The localisable text to be displayed in the cell.
        /// </summary>
        public readonly LocalisableString Header;

        /// <summary>
        /// The anchor of all cells in this column of the <see cref="TableContainer"/>.
        /// </summary>
        public readonly Anchor Anchor;

        /// <summary>
        /// The dimension of the column in the table.
        /// </summary>
        public readonly Dimension Dimension;

        /// <summary>
        /// Constructs a new <see cref="TableColumn"/>.
        /// </summary>
        /// <param name="header">The localisable text to be displayed in the cell.</param>
        /// <param name="anchor">The anchor of all cells in this column of the <see cref="TableContainer"/>.</param>
        /// <param name="dimension">The dimension of the column in the table.</param>
        public TableColumn(LocalisableString? header = null, Anchor anchor = Anchor.TopLeft, Dimension dimension = null)
        {
            Header = header ?? string.Empty;
            Anchor = anchor;
            Dimension = dimension ?? new Dimension();
        }
    }
}
