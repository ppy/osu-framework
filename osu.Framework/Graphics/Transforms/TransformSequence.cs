// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Threading;

namespace osu.Framework.Graphics.Transforms
{
    internal static class TransformSequenceStatics
    {
        private static ulong id = 1;
        public static ulong NextId() => Interlocked.Increment(ref id);
    }

    internal static class TransformSequenceStatics<T>
        where T : class, ITransformable
    {
        /// <summary>
        /// The sequence providing the current context (<see cref="TransformSequence{T}.Merge()"/>).
        /// </summary>
        [ThreadStatic]
        internal static TransformSequence<T>? CurrentContext;
    }

    public readonly struct TransformSequence<T>
        where T : class, ITransformable
    {
        public required T Target { get; init; }
        public required ulong SequenceId { get; init; }
        public required double StartTime { get; init; }

        public int SequenceLength { get; init; }
        public double CurrentTime { get; init; }
        public double EndTime { get; init; }
        public Transform? FirstTransform { get; init; }
        public Transform? LastTransform { get; init; }

        /// <summary>
        /// Creates a new transform sequence.
        /// </summary>
        /// <param name="target">The transform target.</param>
        public static TransformSequence<T> Create(T target)
        {
            if (TransformSequenceStatics<T>.CurrentContext is TransformSequence<T> context)
                return context;

            return new TransformSequence<T>
            {
                Target = target,
                SequenceId = TransformSequenceStatics.NextId(),
                StartTime = target.TransformStartTime,
                CurrentTime = target.TransformStartTime,
                EndTime = target.TransformStartTime
            };
        }

        /// <summary>
        /// Creates a detached orphan of the current sequence, beginning from the current sequence's start time and without awareness of any previously added transforms.
        /// </summary>
        public TransformSequence<T> Orphan() => this with
        {
            SequenceLength = 0,
            EndTime = StartTime,
            FirstTransform = null,
            LastTransform = null
        };

        /// <summary>
        /// Creates a new sequence beginning from the current sequence's end time.
        /// </summary>
        public TransformSequence<T> Then() => this with
        {
            CurrentTime = EndTime
        };

        /// <summary>
        /// Creates a new sequence beginning after a delay from the current sequence.
        /// </summary>
        /// <param name="delay">The amount of time to delay.</param>
        public TransformSequence<T> Delay(double delay) => this with
        {
            CurrentTime = CurrentTime + delay
        };

        /// <summary>
        /// Creates a merge context that allows two sequences to be concatenated.
        /// </summary>
        public MergeContext<T> Merge()
            => new MergeContext<T>(this);

        /// <summary>
        /// Adds a transform to the current sequence and target.
        /// </summary>
        /// <param name="transform">The transform to add.</param>
        public TransformSequence<T> Add(Transform transform)
        {
            if (!ReferenceEquals(transform.TargetTransformable, Target))
            {
                throw new InvalidOperationException(
                    $"{nameof(transform)} must operate upon {nameof(Target)}={Target}, but operates upon {transform.TargetTransformable}.");
            }

            transform.SequenceID = SequenceId;
            transform.SequenceLast = LastTransform;
            transform.EndTime = transform.EndTime - transform.StartTime + CurrentTime;
            transform.StartTime = CurrentTime;

            Target.AddTransform(transform);

            return this with
            {
                SequenceLength = SequenceLength + 1,
                EndTime = Math.Max(EndTime, transform.EndTime),
                FirstTransform = FirstTransform ?? transform,
                LastTransform = transform
            };
        }

        public TransformSequence<T> Finally(Action<T> function)
        {
            OnComplete(function);
            OnAbort(function);
            return this;
        }

        public TransformSequence<T> OnComplete(Action<T> function)
        {
            T t = Target;
            getOrCreateEventHandler().OnComplete += () => function(t);
            return this;
        }

        public TransformSequence<T> OnAbort(Action<T> function)
        {
            T t = Target;
            getOrCreateEventHandler().OnAbort += () => function(t);
            return this;
        }

        /// <summary>
        /// Repeats all transforms within the current sequence indefinitely.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        public TransformSequence<T> Loop(double pause = 0) => Loop(pause, -1);

        /// <summary>
        /// Repeats all transforms within the current sequence.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        public TransformSequence<T> Loop(double pause, int numIters)
        {
            if (SequenceLength == 0)
                return this;

            double iterDuration = EndTime - StartTime + pause;

            Transform[] transformsArr = ArrayPool<Transform>.Shared.Rent(SequenceLength);
            Span<Transform> transforms = transformsArr[..SequenceLength];

            // Traverse backwards from the last transform in the sequence to build a topological list of contained transforms.
            transforms[0] = LastTransform!;
            for (int i = 1; i < SequenceLength; i++)
                transforms[i] = transforms[i - 1].SequenceLast;

            foreach (var t in transforms)
            {
                Target.RemoveTransformNoAbort(t);

                // Update start and end times such that no transformations need to be instantly
                // looped right after they're added. This is required so that transforms can be
                // inserted in the correct order such that none of them trigger abortions on
                // each other due to instant re-sorting upon adding.
                double currentTransformTime = Target.Time.Current;

                int pushForwardCount = 0;

                while (t.EndTime <= currentTransformTime)
                {
                    t.StartTime += iterDuration;
                    t.EndTime += iterDuration;

                    pushForwardCount++;
                }

                // In the finite case, we set LoopCount to the correct value to not add extra unneeded loops
                t.LoopCount = numIters == -1 ? -1 : numIters - pushForwardCount;
            }

            // This sort is required such that no abortions happen.
            transforms.Sort(Transform.COMPARER);

            foreach (var t in transforms)
            {
                t.LoopDelay = iterDuration;

                t.Applied = false;
                t.AppliedToEnd = false; // we want to force a reprocess of this transform. it may have been applied-to-end in the Add, but not correctly looped as a result.

                Target.AddTransform(t);
            }

            ArrayPool<Transform>.Shared.Return(transformsArr);
            return this;
        }

        private TransformSequenceEventHandler getOrCreateEventHandler()
        {
            if (Target.GetTransformEventHandler(SequenceId) is not TransformSequenceEventHandler handler)
                Target.AddTransform(handler = new TransformSequenceEventHandler(Target, SequenceId));
            return handler;
        }

        /// <summary>
        /// A delegate that generates a new <see cref="TransformSequence{T}"/> on a given <paramref name="origin"/>.
        /// </summary>
        /// <param name="origin">The origin to generate a <see cref="TransformSequence{T}"/> for.</param>
        /// <returns>The generated <see cref="TransformSequence{T}"/>.</returns>
        public delegate TransformSequence<T> Generator(T origin);
    }

    public readonly struct MergeContext<T>
        where T : class, ITransformable
    {
        private readonly TransformSequence<T> sequence;
        private readonly TransformSequence<T>? savedContext;

        public MergeContext(TransformSequence<T> sequence)
        {
            this.sequence = sequence;
            savedContext = TransformSequenceStatics<T>.CurrentContext;
            TransformSequenceStatics<T>.CurrentContext = sequence.Orphan();
        }

        /// <summary>
        /// Creates a new sequence that represents the concatenation of a given sequence onto the current context.
        /// </summary>
        /// <param name="target">The sequence to concatenate.</param>
        /// <returns>A new sequence, based off <paramref name="target"/>, representing the concatenation of the two sequences.</returns>
        public TransformSequence<T> With(TransformSequence<T> target)
        {
            TransformSequenceStatics<T>.CurrentContext = savedContext;

            if (target.FirstTransform != null)
                target.FirstTransform.SequenceLast = sequence.LastTransform;

            return target with
            {
                SequenceLength = target.SequenceLength + sequence.SequenceLength,
                EndTime = Math.Max(target.EndTime, sequence.EndTime),
                FirstTransform = sequence.FirstTransform,
                LastTransform = target.LastTransform ?? sequence.LastTransform
            };
        }
    }
}
