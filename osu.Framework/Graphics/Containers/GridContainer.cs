// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using osu.Framework.Caching;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.Containers
{
    public class GridContainer : CompositeDrawable
    {
        private GridDefinition definition;
        public GridDefinition Definition
        {
            get { return definition; }
            set
            {
                if (definition == value)
                    return;
                definition = value;

                layout.Invalidate();
            }
        }

        private Drawable[][] content;
        public Drawable[][] Content
        {
            get { return content; }
            set
            {
                if (content == value)
                    return;
                content = value;

                layout.Invalidate();
            }
        }

        private Cached layout = new Cached();

        protected override void Update()
        {
            base.Update();

            if (!layout.IsValid)
            {
                computeLayout();
                layout.Validate();
            }
        }

        private void computeLayout()
        {
            if (Content == null)
                return;

            ClearInternal();

            int totalColumns = Content.Max(c => c.Length);
            int totalRows = Content.Length;

            var columns = new ColumnInfo[totalColumns];
            var rows = new RowInfo[totalRows];

            float totalWidth = 0;
            float totalHeight = 0;

            // Compute the width of explicitly-defined columns
            if (Definition.Columns?.Count > 0)
            {
                foreach (var d in Definition.Columns)
                {
                    columns[d.Index] = new ColumnInfo();

                    if (d.IsRelative)
                        columns[d.Index].Width = d.Width * DrawWidth;
                    else
                        columns[d.Index].Width = d.Width;
                    totalWidth += columns[d.Index].Width;
                }
            }

            // Compute the height of explicitly-defined rows
            if (Definition.Rows?.Count > 0)
            {
                foreach (var d in Definition.Rows)
                {
                    rows[d.Index] = new RowInfo();

                    if (d.IsRelative)
                        rows[d.Index].Height = d.Height * DrawHeight;
                    else
                        rows[d.Index].Height = d.Height;
                    totalHeight += rows[d.Index].Height;
                }
            }

            // Compute the dimensions for non-explicitly-defined columns/rows
            var autoSize = new Vector2
            (
                (DrawWidth - totalWidth) / totalColumns,
                (DrawHeight - totalHeight) / totalRows
            );

            // Add dimensions to non-explicitly-defined columns/rows and add positional offsets
            if (columns[0] == null)
                columns[0] = new ColumnInfo { Width = autoSize.X };
            if (rows[0] == null)
                rows[0] = new RowInfo { Height = autoSize.Y };

            for (int i = 1; i < columns.Length; i++)
            {
                if (columns[i] == null)
                    columns[i] = new ColumnInfo { Width = autoSize.X };
                columns[i].X = columns[i - 1].X + columns[i - 1].Width;
            }

            for (int i = 1; i < rows.Length; i++)
            {
                if (rows[i] == null)
                    rows[i] = new RowInfo { Height = autoSize.Y };
                rows[i].Y = rows[i - 1].Y + rows[i - 1].Height;
            }

            // Build the cells
            for (int rI = 0; rI < Content.Length; rI++)
            {
                for (int cI = 0; cI < Content[rI].Length; cI++)
                {
                    Vector2 position = new Vector2(columns[cI].X, rows[rI].Y);
                    Vector2 size = new Vector2(columns[cI].Width, rows[rI].Height);

                    AddInternal(new Container
                    {
                        Child = Content[rI][cI],
                        Position = position,
                        Size = size
                    });
                }
            }
        }

        private class ColumnInfo
        {
            public float X;
            public float Width;
        }

        private class RowInfo
        {
            public float Y;
            public float Height;
        }
    }

    /// <summary>
    /// Defines the characteristics of a row in a <see cref="GridContainer"/>.
    /// </summary>
    public class RowDefinition
    {
        /// <summary>
        /// The height of this row.
        /// </summary>
        public float Height;

        /// <summary>
        /// Whether <see cref="Height"/> is relative to the height of the <see cref="GridContainer"/>.
        /// </summary>
        public bool IsRelative;

        /// <summary>
        /// The index of the row which this definition affects.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Constructs a <see cref="RowDefinition"/>.
        /// </summary>
        /// <param name="rowIndex">The index of the row which this definition affects.</param>
        public RowDefinition(int rowIndex)
        {
            Index = rowIndex;
        }
    }

    /// <summary>
    /// Defines the characteristics of a column in a <see cref="GridContainer"/>.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>
        /// The width of this column.
        /// </summary>
        public float Width;

        /// <summary>
        /// Whether <see cref="Width"/> is relative to the width of the <see cref="GridContainer"/>.
        /// </summary>
        public bool IsRelative;

        /// <summary>
        /// The index of the column which this definition affects.
        /// </summary>
        public readonly int Index;

        /// <summary>
        /// Constructs a <see cref="ColumnDefinition"/>.
        /// </summary>
        /// <param name="columnIndex">The index of the column which this definition affects.</param>
        public ColumnDefinition(int columnIndex)
        {
            Index = columnIndex;
        }
    }

    public class GridDefinition
    {
        public IReadOnlyList<RowDefinition> Rows { get; set; }
        public IReadOnlyList<ColumnDefinition> Columns { get; set; }
    }
}
