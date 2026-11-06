// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public class ShelfWithRemainderRectanglePacker : RectanglePacker
    {
        public ShelfWithRemainderRectanglePacker(Vector2I binSize)
            : base(binSize)
        {
        }

        private int x, y, currentShelfHeight;
        private readonly List<RectangleI> freeSpaces = new List<RectangleI>();

        public override void Reset()
        {
            freeSpaces.Clear();
            x = y = currentShelfHeight = 0;
        }

        public override Vector2I? TryAdd(int width, int height)
        {
            Vector2I? result;

            for (int i = 0; i < freeSpaces.Count; i++)
            {
                if (height > freeSpaces[i].Height || width > freeSpaces[i].Width)
                    continue;

                result = freeSpaces[i].TopLeft;

                if (width < freeSpaces[i].Width)
                    freeSpaces[i] = new RectangleI(freeSpaces[i].X + width, freeSpaces[i].Y, freeSpaces[i].Width - width, freeSpaces[i].Height);
                else
                    freeSpaces.RemoveAt(i);

                return result;
            }

            if (y + height > BinSize.Y)
                return null;

            if (x + width > BinSize.X)
            {
                freeSpaces.Add(new RectangleI(x, y, BinSize.X - x, currentShelfHeight));

                x = 0;
                y += currentShelfHeight;
                currentShelfHeight = 0;

                return TryAdd(width, height);
            }

            result = new Vector2I(x, y);

            x += width;
            currentShelfHeight = Math.Max(currentShelfHeight, height);

            return result;
        }

        public override string ToString() => "Shelf (with remainder)";
    }
}