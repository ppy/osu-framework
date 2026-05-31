// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public class ShelfRectanglePacker : RectanglePacker
    {
        public ShelfRectanglePacker(Vector2I binSize)
            : base(binSize)
        {
        }

        private int x, y, currentShelfHeight;

        public override void Reset()
        {
            x = y = currentShelfHeight = 0;
        }

        public override Vector2I? TryAdd(int width, int height)
        {
            if (y + height > BinSize.Y)
                return null;

            if (x + width > BinSize.X)
            {
                x = 0;
                y += currentShelfHeight;
                currentShelfHeight = 0;

                return TryAdd(width, height);
            }

            Vector2I result = new Vector2I(x, y);

            x += width;
            currentShelfHeight = Math.Max(currentShelfHeight, height);

            return result;
        }

        public override string ToString() => "Shelf";
    }
}