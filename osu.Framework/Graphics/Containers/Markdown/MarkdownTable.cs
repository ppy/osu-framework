// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;
using osu.Framework.Allocation;
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
        private GridContainer tableContainer;

        private readonly Table table;

        private readonly Cached columnDefinitionCache = new Cached();
        private readonly Cached rowDefinitionCache = new Cached();

        public MarkdownTable(Table table)
        {
            this.table = table;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            table.Normalize();
        }

        [BackgroundDependencyLoader]
        private void load()
        {
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

            InternalChild = tableContainer = new GridContainer
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
            {
                result &= columnDefinitionCache.Invalidate();
                result &= rowDefinitionCache.Invalidate();
            }

            return result;
        }

        protected override void Update()
        {
            base.Update();

            if (!columnDefinitionCache.IsValid)
            {
                computeColumnDefinitions();
                columnDefinitionCache.Validate();
            }
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!rowDefinitionCache.IsValid)
            {
                computeRowDefinitions();
                rowDefinitionCache.Validate();
            }
        }

        private void computeColumnDefinitions()
        {
            if (table.Count == 0)
                return;

            Span<float> columnWidths = stackalloc float[tableContainer.Content[0].Length];

            // Compute the maximum width of each column
            for (int r = 0; r < tableContainer.Content.Length; r++)
            for (int c = 0; c < tableContainer.Content[r].Length; c++)
                columnWidths[c] = Math.Max(columnWidths[c], ((MarkdownTableCell)tableContainer.Content[r][c]).ContentWidth);

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
                    columnDimensions[i] = new Dimension(GridSizeMode.Relative, columnWidths[i] / totalWidth);
            }

            tableContainer.ColumnDimensions = columnDimensions;
        }

        private void computeRowDefinitions()
        {
            if (table.Count == 0)
                return;

            var rowDefinitions = new Dimension[tableContainer.Content.Length];
            for (int r = 0; r < tableContainer.Content.Length; r++)
                rowDefinitions[r] = new Dimension(GridSizeMode.Absolute, tableContainer.Content[r].Max(c => ((MarkdownTableCell)c).ContentHeight));

            tableContainer.RowDimensions = rowDefinitions;
        }

        protected virtual MarkdownTableCell CreateTableCell(TableCell cell, TableColumnDefinition definition, bool isHeading) => new MarkdownTableCell(cell, definition);
    }
}
