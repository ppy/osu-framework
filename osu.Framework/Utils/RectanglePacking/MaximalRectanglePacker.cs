// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public class MaximalRectanglePacker : FreeSpaceTrackingRectanglePacker
    {
        public MaximalRectanglePacker(Vector2I binSize, FitStrategy strategy)
            : base(binSize, strategy)
        {
        }

        protected override void UpdateFreeSpaces(RectangleI newlyPlaced, int placeIndex)
        {
            for (int i = FreeSpaces.Count - 1; i >= 0; i--)
            {
                if (!newlyPlaced.IntersectsWith(FreeSpaces[i]))
                    continue;

                if (newlyPlaced.Top > FreeSpaces[i].Top && newlyPlaced.Top < FreeSpaces[i].Bottom)
                    FreeSpaces.Add(new RectangleI(FreeSpaces[i].X, FreeSpaces[i].Y, FreeSpaces[i].Width, newlyPlaced.Top - FreeSpaces[i].Top));

                if (newlyPlaced.Bottom >= FreeSpaces[i].Top && newlyPlaced.Bottom < FreeSpaces[i].Bottom)
                    FreeSpaces.Add(new RectangleI(FreeSpaces[i].X, newlyPlaced.Bottom, FreeSpaces[i].Width, FreeSpaces[i].Bottom - newlyPlaced.Bottom));

                if (newlyPlaced.Left > FreeSpaces[i].Left && newlyPlaced.Left < FreeSpaces[i].Right)
                    FreeSpaces.Add(new RectangleI(FreeSpaces[i].X, FreeSpaces[i].Y, newlyPlaced.Left - FreeSpaces[i].Left, FreeSpaces[i].Height));

                if (newlyPlaced.Right >= FreeSpaces[i].Left && newlyPlaced.Right < FreeSpaces[i].Right)
                    FreeSpaces.Add(new RectangleI(newlyPlaced.Right, FreeSpaces[i].Y, FreeSpaces[i].Right - newlyPlaced.Right, FreeSpaces[i].Height));

                FreeSpaces.RemoveAt(i);
            }

            for (int i = FreeSpaces.Count - 1; i >= 0; i--)
            {
                for (int j = FreeSpaces.Count - 1; j >= 0; j--)
                {
                    if (i == j || !FreeSpaces[j].Contains(FreeSpaces[i]))
                        continue;

                    FreeSpaces.RemoveAt(i);
                    break;
                }
            }
        }

        public override string ToString() => $"Maximal ({Strategy})";
    }
}