// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a table.
    /// </summary>
    /// <code>
    /// | heading 1 | heading 2 |
    /// | --------- | --------- |
    /// |  cell 1   |  cell 2   |
    /// </code>
    public class MarkdownTable : CompositeDrawable
    {
        private readonly TableContainer tableContainer;
        private readonly Table table;

        private Cached definitionCache = new Cached();

        public MarkdownTable(Table table)
        {
            this.table = table;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            table.Normalize();

            List<List<Drawable>> rows = new List<List<Drawable>>();

            foreach (var tableRow in table.OfType<TableRow>())
            {
                var row = new List<Drawable>();

                for (int c = 0; c < tableRow.Count; c++)
                {
                    var columnDimensions = table.ColumnDefinitions[c];
                    row.Add(CreateTableCell((TableCell)tableRow[c], columnDimensions, rows.Count == 0));
                }

                rows.Add(row);
            }

            InternalChild = tableContainer = new TableContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Content = rows.Select(x => x.ToArray()).ToArray(),
            };
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            var result = base.Invalidate(invalidation, source, shallPropagate);

            if ((invalidation & Invalidation.DrawSize) > 0 && Parent != null)
                result &= definitionCache.Invalidate();

            return result;
        }

        protected override void Update()
        {
            base.Update();

            validateDefinitions();
        }

        private void validateDefinitions()
        {
            if (!definitionCache.IsValid)
            {
                validateColumnDefinitions();
                validateRowDefinitions();

                definitionCache.Validate();
            }
        }

        private void validateColumnDefinitions()
        {
            if (table.Count == 0)
                return;

            Span<float> columnWidths = stackalloc float[tableContainer.Content[0].Length];

            // Compute the maximum width of each column
            for (int r = 0; r < tableContainer.Content.Length; r++)
            for (int c = 0; c < tableContainer.Content[r].Length; c++)
                columnWidths[c] = Math.Max(columnWidths[c], ((MarkdownTableCell)tableContainer.Content[r][c]).TextFlowContainer.DrawWidth);

            float totalWidth = 0;
            for (int i = 0; i < columnWidths.Length; i++)
                totalWidth += columnWidths[i];

            var columnDimensions = new Dimension[columnWidths.Length];

            if (totalWidth < DrawWidth)
            {
                // The columns will fit within the table, use absolute column widths
                for (int i = 0; i < columnWidths.Length; i++)
                    columnDimensions[i] = new Dimension(GridSizeMode.Absolute, columnWidths[i]);
            }
            else
            {
                // The columns will overflow the table, must convert them to a relative size
                for (int i = 0; i < columnWidths.Length; i++)
                    columnDimensions[i] = new Dimension(GridSizeMode.Relative, totalWidth / columnWidths[i]);
            }

            tableContainer.ColumnDimensions = columnDimensions;
        }

        private void validateRowDefinitions()
        {
            if (table.Count == 0)
                return;

            var rowDefinitions = new Dimension[tableContainer.Content.Length];
            for (int r = 0; r < tableContainer.Content.Length; r++)
                rowDefinitions[r] = new Dimension(GridSizeMode.Absolute, tableContainer.Content[r].Max(c => ((MarkdownTableCell)c).TextFlowContainer.DrawHeight));

            tableContainer.RowDimensions = rowDefinitions;
        }

        protected virtual MarkdownTableCell CreateTableCell(TableCell cell, TableColumnDefinition definition, bool isHeading)
        {
            return new MarkdownTableCell(cell, definition, isHeading);
        }

        private class TableContainer : GridContainer
        {
            public new Axes AutoSizeAxes
            {
                set => base.AutoSizeAxes = value;
            }
        }
    }
}
