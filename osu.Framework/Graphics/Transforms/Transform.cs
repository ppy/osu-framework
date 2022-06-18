// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        public abstract ITransformable TargetTransformable { get; }

        public double StartTime { get; internal set; }
        public double EndTime { get; internal set; }

        public bool IsLooping { get; internal set; }
        public double LoopDelay { get; internal set; }

        public abstract string TargetMember { get; }

        /// <summary>
        /// The name of the grouping this <see cref="Transform"/> belongs to.
        /// Defaults to <see cref="TargetMember"/>.
        /// </summary>
        /// <remarks>
        /// Transforms in a single group affect the same property (or properties) of a <see cref="Transformable"/>.
        /// It is assumed that transforms in different groups are independent from each other
        /// in that they affect different properties, and therefore they can be applied independently
        /// in any order without affecting the end result.
        /// </remarks>
        public virtual string TargetGrouping => TargetMember;

        public abstract void Apply(double time);

        public abstract void ReadIntoStartValue();

        internal bool HasStartValue;

        internal ITransformSequence CompletionTargetSequence;

        internal ITransformSequence AbortTargetSequence;

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

        internal void TriggerComplete() => CompletionTargetSequence?.TransformCompleted();

        internal void TriggerAbort() => AbortTargetSequence?.TransformAborted();
    }

    public abstract class Transform<TValue> : Transform
    {
        public TValue StartValue { get; protected set; }
        public TValue EndValue { get; protected internal set; }
    }

    public abstract class Transform<TValue, TEasing, T> : Transform<TValue>
        where TEasing : IEasingFunction
        where T : class, ITransformable
    {
        public override ITransformable TargetTransformable => Target;

        public T Target { get; internal set; }

        public TEasing Easing { get; internal set; }

        public sealed override void Apply(double time)
        {
            Apply(Target, time);
            Applied = true;
        }

        public sealed override void ReadIntoStartValue() => ReadIntoStartValue(Target);

        protected abstract void Apply(T d, double time);

        protected abstract void ReadIntoStartValue(T d);

        public override string ToString() => $"{Target.GetType().Name}.{TargetMember} {StartTime:0.000}-{EndTime:0.000}ms {StartValue} -> {EndValue}";
    }

    public abstract class Transform<TValue, T> : Transform<TValue, DefaultEasingFunction, T>
        where T : class, ITransformable
    {
    }
}
