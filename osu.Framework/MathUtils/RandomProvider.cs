// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.MathUtils
{
    internal class RandomProvider : IRandomProvider
    {

        private readonly Random _random;

        public RandomProvider(Random random = null)
        {
            _random = random ?? new Random();
        }

        public int Next()
        {
            return _random.Next();
        }

        public int Next(int minValue, int maxValue)
        {
             return _random.Next(minValue, maxValue);
        }

        public int Next(int maxValue)
        {
            return _random.Next(maxValue);
        }

        public void NextBytes(byte[] buffer)
        {
            _random.NextBytes(buffer);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
