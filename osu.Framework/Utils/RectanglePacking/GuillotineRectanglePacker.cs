// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public class GuillotineRectanglePacker : FreeSpaceTrackingRectanglePacker
    {
        private readonly SplitStrategy splitStrategy;

        public GuillotineRectanglePacker(Vector2I binSize, FitStrategy strategy, SplitStrategy splitStrategy)
            : base(binSize, strategy)
        {
            this.splitStrategy = splitStrategy;
        }

        protected override void UpdateFreeSpaces(RectangleI newlyPlaced, int placeIndex)
        {
            RectangleI spaceToDivide = FreeSpaces[placeIndex];

            switch (splitStrategy)
            {
                default:
                case SplitStrategy.ShorterAxis:
                    if (spaceToDivide.Width < spaceToDivide.Height)
                        splitHorizontally(spaceToDivide, newlyPlaced);
                    else
                        splitVertically(spaceToDivide, newlyPlaced);
                    break;

                case SplitStrategy.ShorterLeftoverAxis:
                    if (spaceToDivide.Width - newlyPlaced.Width < spaceToDivide.Height - newlyPlaced.Height)
                        splitHorizontally(spaceToDivide, newlyPlaced);
                    else
                        splitVertically(spaceToDivide, newlyPlaced);
                    break;
            }

            FreeSpaces.RemoveAt(placeIndex);
        }

        private void splitHorizontally(RectangleI spaceToDivide, RectangleI newlyPlaced)
        {
            if (spaceToDivide.Width > newlyPlaced.Width)
                FreeSpaces.Add(new RectangleI(newlyPlaced.Right, spaceToDivide.Y, spaceToDivide.Width - newlyPlaced.Width, newlyPlaced.Height));

            if (spaceToDivide.Height > newlyPlaced.Height)
                FreeSpaces.Add(new RectangleI(spaceToDivide.X, newlyPlaced.Bottom, spaceToDivide.Width, spaceToDivide.Height - newlyPlaced.Height));
        }

        private void splitVertically(RectangleI spaceToDivide, RectangleI newlyPlaced)
        {
            if (spaceToDivide.Height > newlyPlaced.Height)
                FreeSpaces.Add(new RectangleI(spaceToDivide.X, newlyPlaced.Bottom, newlyPlaced.Width, spaceToDivide.Height - newlyPlaced.Height));

            if (spaceToDivide.Width > newlyPlaced.Width)
                FreeSpaces.Add(new RectangleI(newlyPlaced.Right, spaceToDivide.Y, spaceToDivide.Width - newlyPlaced.Width, spaceToDivide.Height));
        }

        public override string ToString() => $"Guillotine ({Strategy}, {splitStrategy})";
    }

    public enum SplitStrategy
    {
        ShorterAxis,
        ShorterLeftoverAxis
    }
}