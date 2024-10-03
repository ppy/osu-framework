// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using osu.Framework.Extensions.TypeExtensions;

namespace osu.Framework.Graphics.Transforms
{
    internal static class TransformSequenceHelpers
    {
        private static ulong id = 1;

        public static ulong GetNextId() => Interlocked.Increment(ref id);
    }

    public readonly struct TransformSequence<T>
        where T : class, ITransformable
    {
        /// <summary>
        /// A unique identifier for the sequence.
        /// </summary>
        public required ulong Id { get; init; }

        /// <summary>
        /// The target to be transformed.
        /// </summary>
        public required T Target { get; init; }

        /// <summary>
        /// The time at which the sequence begins.
        /// </summary>
        public double StartTime { get; init; }

        /// <summary>
        /// The time at which any transforms added to the sequence will start from.
        /// </summary>
        public double CurrentTime { get; init; }

        /// <summary>
        /// The time at which the sequence ends.
        /// </summary>
        public double EndTime { get; init; }

        /// <summary>
        /// The number of transforms in the sequence and any of its continuations.
        /// </summary>
        public int Length { get; init; }

        /// <summary>
        /// The first transform added to the sequence.
        /// </summary>
        public Transform? FirstTransform { get; init; }

        /// <summary>
        /// The last transform added to the sequence.
        /// </summary>
        public Transform? LastTransform { get; init; }

        /// <summary>
        /// Creates a blank transform sequence.
        /// </summary>
        /// <param name="target">The transform target.</param>
        public static TransformSequence<T> Create(T target)
        {
            if (ContinuationContext<T>.Current is TransformSequence<T> continuation)
                return continuation;

            return new TransformSequence<T>
            {
                Id = TransformSequenceHelpers.GetNextId(),
                Target = target,
                StartTime = target.TransformStartTime,
                CurrentTime = target.TransformStartTime,
                EndTime = target.TransformStartTime
            };
        }

        /// <summary>
        /// Creates a transform sequence from an existing transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        public static TransformSequence<T> Create(Transform transform)
        {
            TransformSequenceException.ThrowIfInvalidTransform<T>(transform);

            T target = (T)transform.Target;
            TransformSequence<T> sequence = Create(target);

            transform.SequenceID = sequence.Id;
            transform.PreviousInSequence = sequence.LastTransform;
            transform.EndTime = sequence.CurrentTime + transform.EndTime - transform.StartTime;
            transform.StartTime = sequence.CurrentTime;

            target.AddTransform(transform);

            return sequence with
            {
                EndTime = Math.Max(sequence.EndTime, transform.EndTime),
                Length = sequence.Length + 1,
                FirstTransform = sequence.FirstTransform ?? transform,
                LastTransform = transform
            };
        }

        /// <summary>
        /// Continues from the end time.
        /// </summary>
        public TransformSequence<T> Then() => this with
        {
            CurrentTime = EndTime
        };

        /// <summary>
        /// Continues after a delay.
        /// </summary>
        /// <param name="delay">The amount of time to delay.</param>
        public TransformSequence<T> Delay(double delay) => this with
        {
            CurrentTime = CurrentTime + delay
        };

        /// <summary>
        /// Continues from the current time as an empty sequence.
        /// </summary>
        public TransformSequence<T> Branch() => this with
        {
            StartTime = CurrentTime,
            CurrentTime = CurrentTime,
            EndTime = CurrentTime,
            Length = 0,
            FirstTransform = null,
            LastTransform = null
        };

        /// <summary>
        /// Repeats all added transforms indefinitely.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        public TransformSequence<T> Loop(double pause = 0) => Loop(pause, -1);

        /// <summary>
        /// Repeats all added transforms.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        public TransformSequence<T> Loop(double pause, int numIters)
        {
            if (Length == 0)
                return this;

            double iterDuration = EndTime - StartTime + pause;

            Transform[] transformsArr = ArrayPool<Transform>.Shared.Rent(Length);
            Span<Transform> transforms = transformsArr[..Length];

            // Traverse backwards from the last transform in the sequence to build a topological list of contained transforms.
            transforms[0] = LastTransform!;
            for (int i = 1; i < Length; i++)
                transforms[i] = transforms[i - 1].PreviousInSequence;

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

        /// <summary>
        /// Creates a continuation context for the current sequence.
        /// </summary>
        public ContinuationContext<T> CreateContinuation()
            => new ContinuationContext<T>(this);

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

        private TransformSequenceEventHandler getOrCreateEventHandler()
        {
            if (Target.GetTransformEventHandler(Id) is not TransformSequenceEventHandler handler)
                Target.AddTransform(handler = new TransformSequenceEventHandler(Target, Id));
            return handler;
        }

        /// <summary>
        /// A delegate that generates a new <see cref="TransformSequence{T}"/> on a given <paramref name="origin"/>.
        /// </summary>
        /// <param name="origin">The origin to generate a <see cref="TransformSequence{T}"/> for.</param>
        /// <returns>The generated <see cref="TransformSequence{T}"/>.</returns>
        public delegate TransformSequence<T> Generator(T origin);
    }

    public readonly struct ContinuationContext<T>
        where T : class, ITransformable
    {
        /// <summary>
        /// The current continuation sequence, if any.
        /// </summary>
        internal static TransformSequence<T>? Current => tls_current.Value;

        /// <summary>
        /// Thread local storage for the current continuation sequence.
        /// </summary>
        private static readonly ThreadLocal<TransformSequence<T>?> tls_current = new ThreadLocal<TransformSequence<T>?>();

        /// <summary>
        /// The previous continuation context sequence.
        /// </summary>
        private readonly TransformSequence<T>? savedContext;

        /// <summary>
        /// The sequence that this context originated from.
        /// </summary>
        private readonly TransformSequence<T> origin;

        /// <summary>
        /// Creates a continuation context.
        /// </summary>
        /// <param name="origin">The originating sequence.</param>
        public ContinuationContext(TransformSequence<T> origin)
        {
            this.origin = origin;

            // Set a continuation context rooted at the leading sequence.
            savedContext = tls_current.Value;
            tls_current.Value = origin.Branch();
        }

        /// <summary>
        /// Appends a sequence to the continuation.
        /// </summary>
        /// <param name="continuation">The sequence to append.</param>
        /// <returns>A new sequence based off the original with transforms appended from the continuation target.</returns>
        public TransformSequence<T> Append(TransformSequence<T> continuation)
        {
            // The current context is only required during construction of the continuation, restore the old one.
            tls_current.Value = savedContext;

            // Link the continuation to the leader.
            if (continuation.FirstTransform != null)
                continuation.FirstTransform.PreviousInSequence = origin.LastTransform;

            // Link the leader to the continuation.
            return origin with
            {
                EndTime = Math.Max(origin.EndTime, continuation.EndTime),
                Length = origin.Length + continuation.Length,
                LastTransform = continuation.LastTransform ?? origin.LastTransform
            };
        }
    }

    public class TransformSequenceException : Exception
    {
        public TransformSequenceException(string message)
            : base(message)
        {
        }

        public static void ThrowIfInvalidTransform<T>(Transform transform)
            where T : class, ITransformable
        {
            if (transform.Target is null)
                throwTargetIsNull();

            if (transform.Target is not T)
                throwTargetTypeMismatch(typeof(T), transform.Target.GetType());
        }

        [DoesNotReturn]
        private static void throwTargetIsNull()
            => throw new TransformSequenceException("Transform target cannot be null.");

        [DoesNotReturn]
        private static void throwTargetTypeMismatch(Type expected, Type actual)
            => throw new TransformSequenceException($"Transform target was expected to be of type '{expected.ReadableName()}' but was '{actual.ReadableName()}'.");
    }
}
