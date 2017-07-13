// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransform
    {
        long CreationID { get; }

        double Duration { get; }

        double StartTime { get; set; }
        double EndTime { get; set; }

        void Apply();

        void ReadIntoStartValue();

        void Loop(double delay, int loopCount = -1);

        void NextIteration();

        bool HasNextIteration { get; }

        void UpdateTime(FrameTimeInfo time);

        FrameTimeInfo? Time { get; }

        Action<double> OnComplete { get; set; }

        Action<double> OnAbort { get; set; }
    }

    public class TransformTimeComparer : IComparer<ITransform>
    {
        public int Compare(ITransform x, ITransform y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            int compare = x.StartTime.CompareTo(y.StartTime);
            if (compare != 0) return compare;
            compare = x.CreationID.CompareTo(y.CreationID);
            return compare;
        }
    }
}
