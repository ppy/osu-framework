// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;
using osu.Framework.Allocation;
using osu.Framework.Layout;

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
    public partial class MarkdownTable : CompositeDrawable
    {
        private GridContainer tableContainer;

        private readonly Table table;

        private readonly LayoutValue columnDefinitionCache = new LayoutValue(Invalidation.DrawSize, conditions: (s, _) => s.Parent != null);
        private readonly LayoutValue rowDefinitionCache = new LayoutValue(Invalidation.DrawSize, conditions: (s, _) => s.Parent != null);

        public MarkdownTable(Table table)
        {
            this.table = table;

            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;

            table.NormalizeUsingHeaderRow();

            AddLayout(columnDefinitionCache);
            AddLayout(rowDefinitionCache);
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
                Content = rows.Select(x => x.ToArray()).ToArray()
            };
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

            int columnCount = tableContainer.Content[0].Count;
            Span<float> maxColumnWidths = stackalloc float[columnCount];
            Span<float> minColumnWidths = stackalloc float[columnCount];

            // Compute the maximum and minimum width of each column
            for (int r = 0; r < tableContainer.Content.Count; r++)
            {
                for (int c = 0; c < tableContainer.Content[r].Count; c++)
                {
                    MarkdownTableCell tableCell = (MarkdownTableCell)tableContainer.Content[r][c];
                    maxColumnWidths[c] = Math.Max(maxColumnWidths[c], tableCell.ContentWidth);
                    minColumnWidths[c] = Math.Max(minColumnWidths[c], tableCell.MinimumContentWidth);
                }
            }

            float totalWidth = 0;
            for (int i = 0; i < columnCount; i++)
                totalWidth += maxColumnWidths[i];

            var columnDimensions = new Dimension[columnCount];

            if (totalWidth < DrawWidth)
            {
                // The columns will fit within the table, use absolute column widths
                for (int i = 0; i < columnCount; i++)
                    columnDimensions[i] = new Dimension(GridSizeMode.Absolute, maxColumnWidths[i]);
            }
            else
            {
                Span<float> targetColumnWidths = stackalloc float[columnCount];
                float totalTargetWidth = 0;

                for (int i = 0; i < columnCount; i++)
                {
                    targetColumnWidths[i] = minColumnWidths[i];
                    totalTargetWidth += targetColumnWidths[i];
                }

                if (totalTargetWidth < DrawWidth)
                {
                    float remainingWidth = DrawWidth - totalTargetWidth;

                    for (int i = 0; i < columnCount; i++)
                        targetColumnWidths[i] += (maxColumnWidths[i] / totalWidth) * remainingWidth;
                }

                // The columns will overflow the table, must convert them to a relative size
                for (int i = 0; i < columnCount; i++)
                    columnDimensions[i] = new Dimension(GridSizeMode.Relative, targetColumnWidths[i] / DrawWidth);
            }

            tableContainer.ColumnDimensions = columnDimensions;
        }

        private void computeRowDefinitions()
        {
            if (table.Count == 0)
                return;

            var rowDefinitions = new Dimension[tableContainer.Content.Count];
            for (int r = 0; r < tableContainer.Content.Count; r++)
                rowDefinitions[r] = new Dimension(GridSizeMode.Absolute, tableContainer.Content[r].Max(c => ((MarkdownTableCell)c).ContentHeight));

            tableContainer.RowDimensions = rowDefinitions;
        }

        protected virtual MarkdownTableCell CreateTableCell(TableCell cell, TableColumnDefinition definition, bool isHeading) => new MarkdownTableCell(cell, definition);
    }
}
