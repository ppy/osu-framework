// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osu.Framework.Utils.RectanglePacking;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkRectanglePacking
    {
        private ShelfRectanglePacker shelf = null!;
        private ShelfWithRemainderRectanglePacker shelfRemainder = null!;
        private MaximalRectanglePacker maximal = null!;
        private GuillotineRectanglePacker guillotine = null!;

        private readonly List<Vector2I> rects = new List<Vector2I>();

        [GlobalSetup]
        public void GlobalSetup()
        {
            for (int i = 0; i < 30; i++)
                rects.Add(new Vector2I(RNG.Next(20, 30), RNG.Next(20, 30)));
        }

        [Benchmark]
        public void PopulateShelf()
        {
            shelf = new ShelfRectanglePacker(new Vector2I(1024));

            for (int i = 0; i < rects.Count; i++)
                shelf.TryAdd(rects[i].X, rects[i].Y);
        }

        [Benchmark]
        public void PopulateShelfRemainder()
        {
            shelfRemainder = new ShelfWithRemainderRectanglePacker(new Vector2I(1024));

            for (int i = 0; i < rects.Count; i++)
                shelfRemainder.TryAdd(rects[i].X, rects[i].Y);
        }

        [Benchmark]
        public void PopulateMaximal()
        {
            maximal = new MaximalRectanglePacker(new Vector2I(1024), FitStrategy.BestShortSide);

            for (int i = 0; i < rects.Count; i++)
                maximal.TryAdd(rects[i].X, rects[i].Y);
        }

        [Benchmark]
        public void PopulateGuillotine()
        {
            guillotine = new GuillotineRectanglePacker(new Vector2I(1024), FitStrategy.SmallestArea, SplitStrategy.ShorterLeftoverAxis);

            for (int i = 0; i < rects.Count; i++)
                guillotine.TryAdd(rects[i].X, rects[i].Y);
        }
    }
}
