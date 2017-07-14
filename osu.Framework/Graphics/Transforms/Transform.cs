// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;
using System;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transform<T> : ITransform
    {
        public ulong TransformID { get; internal set; }

        private readonly T target;

        protected Transform(T target)
        {
            this.target = target;
        }

        public double Duration => EndTime - StartTime;

        public EasingTypes Easing;

        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public void Apply() => Apply(target);

        public void ReadIntoStartValue() => ReadIntoStartValue(target);

        public abstract void Apply(T d);

        public abstract void ReadIntoStartValue(T d);

        public void UpdateTime(FrameTimeInfo time)
        {
            Time = time;
        }

        public FrameTimeInfo? Time { get; private set; }

        public Action<double> OnComplete { get; set; }

        public Action<double> OnAbort { get; set; }
    }

    public abstract class Transform<TValue, T> : Transform<T>
    {
        protected Transform(T target) : base(target)
        {
        }

        public TValue StartValue { get; protected set; }
        public TValue EndValue { get; set; }

        public override string ToString()
        {
            return string.Format("Transform({2}) {0}-{1}", StartTime, EndTime, typeof(TValue));
        }
    }
}
