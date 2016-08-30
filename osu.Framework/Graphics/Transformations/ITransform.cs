//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Transformations
{
    public interface ITransform
    {
        double Duration { get; }
        bool IsAlive { get; }

        double StartTime { get; set; }
        double EndTime { get; set; }

        void Apply(Drawable d);

        ITransform Clone();
        ITransform CloneReverse();

        void Reverse();
        void Loop(double delay, int loopCount = 0);
    }
}