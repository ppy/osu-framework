// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransform : IHasLifetime
    {
        double Duration { get; }

        double StartTime { get; set; }
        double EndTime { get; set; }

        void Apply(Drawable d);

        ITransform Clone();
        ITransform CloneReverse();

        void Reverse();
        void Loop(double delay, int loopCount = -1);

        /// <summary>
        /// Shift this transform by the specified time value.
        /// </summary>
        /// <param name="offset">Time in milliseconds to shift the transform.</param>
        void Shift(double offset);
    }

    public class TransformTimeComparer : IComparer<ITransform>
    {
        public int Compare(ITransform x, ITransform y)
        {
            if (x == null) throw new ArgumentNullException(nameof(x));
            if (y == null) throw new ArgumentNullException(nameof(y));

            int compare = x.StartTime.CompareTo(y.StartTime);
            if (compare != 0) return compare;
            compare = x.EndTime.CompareTo(y.EndTime);
            return compare;
        }
    }
}
