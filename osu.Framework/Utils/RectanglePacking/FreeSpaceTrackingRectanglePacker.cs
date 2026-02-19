// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public abstract class FreeSpaceTrackingRectanglePacker : RectanglePacker
    {
        protected readonly List<RectangleI> FreeSpaces = new List<RectangleI>();
        protected readonly FitStrategy Strategy;

        protected FreeSpaceTrackingRectanglePacker(Vector2I binSize, FitStrategy strategy)
            : base(binSize)
        {
            Strategy = strategy;
        }

        public override void Reset()
        {
            FreeSpaces.Clear();
            FreeSpaces.Add(new RectangleI(0, 0, BinSize.X, BinSize.Y));
        }

        public override Vector2I? TryAdd(int width, int height)
        {
            RectangleI? bestFit = null;
            int bestFitIndex = 0;

            for (int i = 0; i < FreeSpaces.Count; i++)
            {
                if (FreeSpaces[i].Width < width || FreeSpaces[i].Height < height)
                    continue;

                if (Strategy == FitStrategy.First)
                {
                    bestFit = FreeSpaces[i];
                    bestFitIndex = i;
                    break;
                }

                if (bestFit.HasValue && !isBetterFit(bestFit.Value, FreeSpaces[i]))
                    continue;

                bestFit = FreeSpaces[i];
                bestFitIndex = i;
            }

            if (!bestFit.HasValue)
                return null;

            UpdateFreeSpaces(new RectangleI(bestFit.Value.X, bestFit.Value.Y, width, height), bestFitIndex);
            return bestFit.Value.TopLeft;
        }

        protected abstract void UpdateFreeSpaces(RectangleI newlyPlaced, int placeIndex);

        private bool isBetterFit(RectangleI currentBest, RectangleI potentialBest)
        {
            switch (Strategy)
            {
                default:
                case FitStrategy.First:
                    return false;

                case FitStrategy.TopLeft:
                    return potentialBest.Y < currentBest.Y || (potentialBest.Y == currentBest.Y && potentialBest.X < currentBest.X);

                case FitStrategy.BestLongSide:
                    return Math.Max(potentialBest.Height, potentialBest.Width) < Math.Max(currentBest.Height, currentBest.Width);

                case FitStrategy.BestShortSide:
                    return Math.Min(potentialBest.Height, potentialBest.Width) < Math.Min(currentBest.Height, currentBest.Width);

                case FitStrategy.SmallestArea:
                    return potentialBest.Area < currentBest.Area;
            }
        }
    }

    public enum FitStrategy
    {
        First,
        TopLeft,
        SmallestArea,
        BestShortSide,
        BestLongSide
    }
}