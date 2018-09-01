// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using Markdig.Extensions.Tables;

namespace osu.Framework.Graphics.Containers.Markdown
{
    /// <summary>
    /// Visualises a markdown table, containing <see cref="MarkdownTableCell"/>s.
    /// </summary>
    public class MarkdownTable : CompositeDrawable
    {
        private readonly MarkdownTableContainer tableContainer;
        private readonly List<List<MarkdownTableCell>> listContainerArray = new List<List<MarkdownTableCell>>();

        public MarkdownTable(Table table)
        {
            AutoSizeAxes = Axes.Y;
            RelativeSizeAxes = Axes.X;
            Padding = new MarginPadding { Right = 100 };
            Margin = new MarginPadding { Right = 100 };

            foreach (var block in table)
            {
                var tableRow = (TableRow)block;
                List<MarkdownTableCell> rows = new List<MarkdownTableCell>();

                if (tableRow != null)
                    for (int columnIndex = 0; columnIndex < tableRow.Count; columnIndex++)
                    {
                        var columnDimensions = table.ColumnDefinitions[columnIndex];
                        var tableCell = (TableCell)tableRow[columnIndex];
                        if (tableCell != null)
                            rows.Add(CreateMarkdownTableCell(tableCell, columnDimensions, listContainerArray.Count, columnIndex));
                    }

                listContainerArray.Add(rows);
            }

            InternalChild = tableContainer = new MarkdownTableContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Content = listContainerArray.Select(x => x.Select(y => (Drawable)y).ToArray()).ToArray(),
            };
        }

        /*
        private Vector2 lastDrawSize;
        protected override void Update()
        {
            if (lastDrawSize != DrawSize)
            {
                lastDrawSize = DrawSize;
                updateColumnDefinitions();
                updateRowDefinitions();
            }
            base.Update();
        }
        */

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            var reault = base.Invalidate(invalidation, source, shallPropagate);
            if ((invalidation & Invalidation.DrawSize) > 0)
            {
                updateColumnDefinitions();
                updateRowDefinitions();
            }
            
            return reault;
        }

        private void updateColumnDefinitions()
        {
            if(!listContainerArray.Any())
                return;

            var totalColumn = listContainerArray.Max(x => x.Count);
            var totalRows = listContainerArray.Count;

            var listcolumnMaxWidth = new float[totalColumn];

            for (int row = 0; row < totalRows; row++)
            {
                for (int column = 0; column < totalColumn; column++)
                {
                    var colimnTextTotalWidth = listContainerArray[row][column].TextFlowContainer.TotalTextWidth;

                    //get max width
                    listcolumnMaxWidth[column] = Math.Max(listcolumnMaxWidth[column], colimnTextTotalWidth);
                }
            }

            listcolumnMaxWidth = listcolumnMaxWidth.Select(x => x + 20).ToArray();

            var columnDimensions = new Dimension[totalColumn];

            //if max width < DrawWidth, means set absolute value to each column
            if (listcolumnMaxWidth.Sum() < DrawWidth - Margin.Right)
            {
                //not relative , define value instead
                tableContainer.RelativeSizeAxes = Axes.None;
                for (int column = 0; column < totalColumn; column++)
                {
                    columnDimensions[column] = new Dimension(GridSizeMode.Absolute, listcolumnMaxWidth[column]);
                }
            }
            else
            {
                //set to relative
                tableContainer.RelativeSizeAxes = Axes.X;
                var totalWidth = listcolumnMaxWidth.Sum();
                for (int column = 0; column < totalColumn; column++)
                {
                    columnDimensions[column] = new Dimension(GridSizeMode.Relative, listcolumnMaxWidth[column] / totalWidth);
                }
            }
            tableContainer.ColumnDimensions = columnDimensions;
        }

        private void updateRowDefinitions()
        {
            if (!listContainerArray.Any())
                return;

            tableContainer.RowDimensions = listContainerArray.Select(x => new Dimension(GridSizeMode.Absolute, x.Max(y => y.TextFlowContainer.DrawHeight + 10))).ToArray();
        }

        protected virtual MarkdownTableCell CreateMarkdownTableCell(TableCell cell, TableColumnDefinition definition, int rowNumber,int columnNumber)
        {
            return new MarkdownTableCell(cell, definition, rowNumber, columnNumber);
        }

        private class MarkdownTableContainer : GridContainer
        {
            public new Axes AutoSizeAxes
            {
                set => base.AutoSizeAxes = value;
            }
        }
    }
}
