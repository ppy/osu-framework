// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.MathUtils
{
    public interface IRandomProvider
    {
        int Next();
        int Next(int minValue, int maxValue);
        int Next(int maxValue);
        void NextBytes(byte[] buffer);
        double NextDouble();
    }
}
