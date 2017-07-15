// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transform
    {
        internal ulong TransformID { private get; set; }

        public double Duration => EndTime - StartTime;

        public EasingTypes Easing;

        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public abstract void Apply();

        public abstract void ReadIntoStartValue();

        public void UpdateTime(FrameTimeInfo time)
        {
            Time = time;
        }

        public FrameTimeInfo? Time { get; private set; }

        public Action<double> OnComplete { get; set; }

        public Action<double> OnAbort { get; set; }


        public static readonly IComparer<Transform> COMPARER = new TransformTimeComparer();

        private class TransformTimeComparer : IComparer<Transform>
        {
            public int Compare(Transform x, Transform y)
            {
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

                int compare = x.StartTime.CompareTo(y.StartTime);
                if (compare != 0) return compare;
                compare = x.TransformID.CompareTo(y.TransformID);
                return compare;
            }
        }
    }

    public abstract class Transform<T> : Transform
    {
        public T Target;

        public sealed override void Apply() => Apply(Target);

        public sealed override void ReadIntoStartValue() => ReadIntoStartValue(Target);

        public abstract void Apply(T d);

        public abstract void ReadIntoStartValue(T d);
    }

    public abstract class Transform<TValue, T> : Transform<T>
    {
        public TValue StartValue { get; protected set; }
        public TValue EndValue { get; set; }

        public override string ToString()
        {
            return string.Format("Transform({2}) {0}-{1}", StartTime, EndTime, typeof(TValue));
        }
    }
}
