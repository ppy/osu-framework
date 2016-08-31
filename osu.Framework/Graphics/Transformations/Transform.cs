//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transformations
{
    public abstract class Transform<T> : ITransform, IComparable<Transform<T>>
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public T StartValue { get; set; }
        public T EndValue { get; set; }

        public double LoopDelay;
        public int LoopCount;
        public int CurrentLoopCount;

        public EasingTypes Easing;

        public double Duration => EndTime - StartTime;

        protected IClock Clock;

        protected double Time => Clock.CurrentTime;

        public bool IsAlive => StartTime <= Clock.CurrentTime && EndTime >= Clock.CurrentTime;

        public Transform(IClock clock)
        {
            Clock = clock;
        }

        public ITransform Clone()
        {
            return (ITransform)MemberwiseClone();
        }

        public ITransform CloneReverse()
        {
            ITransform t = Clone();
            t.Reverse();
            return t;
        }

        public virtual void Reverse()
        {
            var tmp = StartValue;
            StartValue = EndValue;
            EndValue = tmp;
        }

        public void Loop(double delay, int loopCount = -1)
        {
            LoopDelay = delay;
            LoopCount = loopCount;
        }

        public abstract T CurrentValue { get; }

        public abstract void Apply(Drawable d);

        public int CompareTo(Transform<T> other)
        {
            int compare = StartTime.CompareTo(other.StartTime);
            if (compare != 0) return compare;
            compare = EndTime.CompareTo(other.EndTime);
            return compare;
        }

        public override string ToString()
        {
            return string.Format("Transform({2}) {0}-{1}", StartTime, EndTime, typeof(T));
        }
    }
}
