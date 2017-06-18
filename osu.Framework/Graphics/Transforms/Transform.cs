// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transform<TValue, T> : ITransform<T>
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }

        public TValue StartValue { get; set; }
        public abstract TValue CurrentValue { get; }
        public TValue EndValue { get; set; }

        public double LoopDelay;
        public int LoopCount;
        public int CurrentLoopCount;

        public EasingTypes Easing;

        public double Duration => EndTime - StartTime;

        protected FrameTimeInfo? Time;

        public void UpdateTime(FrameTimeInfo time)
        {
            Time = time;
        }

        public bool IsAlive
        {
            get
            {
                //we may not have reached the start of this transform yet.
                if (StartTime > Time?.Current)
                    return false;

                return EndTime >= Time?.Current || LoopCount != CurrentLoopCount;
            }
        }

        public ITransform<T> Clone()
        {
            return (ITransform<T>)MemberwiseClone();
        }

        public ITransform<T> CloneReverse()
        {
            ITransform<T> t = Clone();
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

        public double LifetimeStart => StartTime;

        public double LifetimeEnd => EndTime;

        public bool LoadRequired => false;

        public bool RemoveWhenNotAlive => Time?.Current > EndTime;

        public bool IsLoaded => true;

        public virtual void Apply(T d)
        {
            if (Time?.Current > EndTime && LoopCount != CurrentLoopCount)
            {
                CurrentLoopCount++;
                double duration = Duration;
                StartTime = EndTime + LoopDelay;
                EndTime = StartTime + duration;
            }
        }

        public override string ToString()
        {
            return string.Format("Transform({2}) {0}-{1}", StartTime, EndTime, typeof(TValue));
        }

        public void Load()
        {
        }

        public void Shift(double offset)
        {
            StartTime += offset;
            EndTime += offset;
        }
    }
}
