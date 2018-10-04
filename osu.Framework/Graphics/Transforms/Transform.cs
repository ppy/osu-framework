// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class Transform
    {
        internal ulong TransformID;

        /// <summary>
        /// Whether this <see cref="Transform"/> has been applied to an <see cref="ITransformable"/>.
        /// </summary>
        internal bool Applied;

        /// <summary>
        /// Whether this <see cref="Transform"/> has been applied completely to an <see cref="ITransformable"/>.
        /// Used to track whether we still need to apply for targets which allow rewind.
        /// </summary>
        internal bool AppliedToEnd;

        /// <summary>
        /// Whether this <see cref="Transform"/> can be rewound.
        /// </summary>
        public bool Rewindable = true;

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
                if (x == null) throw new ArgumentNullException(nameof(x));
                if (y == null) throw new ArgumentNullException(nameof(y));

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

        public override string ToString() => $"{Target.GetType().Name}.{TargetMember} {StartTime}-{EndTime}ms {StartValue} -> {EndValue}";
    }
}
