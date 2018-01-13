// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using System;
using System.Collections.Generic;
using Guards;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transform
    {
        internal ulong TransformID;

        /// <summary>
        /// Whether this <see cref="Transform"/> has been applied to an <see cref="ITransformable"/>.
        /// </summary>
        internal bool Applied;

        public Easing Easing;

        public abstract ITransformable TargetTransformable { get; }

        public double StartTime { get; internal set; }
        public double EndTime { get; internal set; }

        public bool IsLooping { get; internal set; }
        public double LoopDelay { get; internal set; }

        public abstract string TargetMember { get; }

        public abstract void Apply(double time);

        public abstract void ReadIntoStartValue();

        internal bool HasStartValue;

        public Action OnComplete;

        public Action OnAbort;

        public Transform Clone() => (Transform)MemberwiseClone();

        public static readonly IComparer<Transform> COMPARER = new TransformTimeComparer();

        private class TransformTimeComparer : IComparer<Transform>
        {
            public int Compare(Transform x, Transform y)
            {
                Guard.ArgumentNotNull(x, nameof(x));
                Guard.ArgumentNotNull(y, nameof(y));

                int compare = x.StartTime.CompareTo(y.StartTime);
                if (compare != 0) return compare;
                compare = x.TransformID.CompareTo(y.TransformID);
                return compare;
            }
        }
    }

    public abstract class Transform<TValue> : Transform
    {
        public TValue StartValue { get; protected set; }
        public TValue EndValue { get; protected internal set; }
    }

    public abstract class Transform<TValue, T> : Transform<TValue>
        where T : ITransformable
    {
        public override ITransformable TargetTransformable => Target;

        public T Target { get; internal set; }

        public sealed override void Apply(double time)
        {
            Apply(Target, time);
            Applied = true;
        }

        public sealed override void ReadIntoStartValue() => ReadIntoStartValue(Target);

        protected abstract void Apply(T d, double time);

        protected abstract void ReadIntoStartValue(T d);

        public override string ToString() => $"{typeof(Transform<TValue, T>).ReadableName()} => {Target} {StartTime}:{StartValue}-{EndTime}:{EndValue}";
    }
}
