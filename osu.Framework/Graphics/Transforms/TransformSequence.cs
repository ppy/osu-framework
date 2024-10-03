// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using JetBrains.Annotations;
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
        /// Thread local storage for the current continuation sequence.
        /// </summary>
        private static readonly ThreadLocal<TransformSequence<T>?> tls_current_context = new ThreadLocal<TransformSequence<T>?>();

        /// <summary>
        /// A unique identifier for the sequence.
        /// </summary>
        private ulong id { get; init; }

        /// <summary>
        /// The target to be transformed.
        /// </summary>
        private T target { get; init; }

        /// <summary>
        /// The time at which the sequence begins.
        /// </summary>
        private double startTime { get; init; }

        /// <summary>
        /// The time at which any transforms added to the sequence will start from.
        /// </summary>
        private double currentTime { get; init; }

        /// <summary>
        /// The time at which the sequence ends.
        /// </summary>
        private double endTime { get; init; }

        /// <summary>
        /// The number of transforms in the sequence and any of its continuations.
        /// </summary>
        private int length { get; init; }

        /// <summary>
        /// The transform last added to the sequence.
        /// </summary>
        private Transform? transform { get; init; }

        /// <summary>
        /// Creates a blank transform sequence.
        /// </summary>
        /// <param name="target">The transform target.</param>
        public static TransformSequence<T> Create(T target)
        {
            // Consume the global context, if any.
            if (tls_current_context.Value is TransformSequence<T> context)
            {
                tls_current_context.Value = null;
                return context;
            }

            return new TransformSequence<T>
            {
                id = TransformSequenceHelpers.GetNextId(),
                target = target,
                startTime = target.TransformStartTime,
                currentTime = target.TransformStartTime,
                endTime = target.TransformStartTime
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

            transform.SequenceID = sequence.id;
            transform.PreviousInSequence = sequence.transform;
            transform.EndTime = sequence.currentTime + transform.EndTime - transform.StartTime;
            transform.StartTime = sequence.currentTime;

            target.AddTransform(transform);

            return sequence with
            {
                endTime = Math.Max(sequence.endTime, transform.EndTime),
                length = sequence.length + 1,
                transform = transform
            };
        }

        /// <summary>
        /// Continues with an action on the target.
        /// </summary>
        [MustUseReturnValue]
        public T Continue()
        {
            tls_current_context.Value = this;
            return target;
        }

        /// <summary>
        /// Continues with an action on the target.
        /// </summary>
        [MustUseReturnValue]
        public T Continue(out T target)
        {
            tls_current_context.Value = this;
            target = this.target;
            return target;
        }

        /// <summary>
        /// Continues with an action on the target.
        /// </summary>
        [MustUseReturnValue]
        public TransformSequence<T> Continue(Generator generator)
        {
            tls_current_context.Value = this;
            return generator(target);
        }

        /// <summary>
        /// Continues from the current end time.
        /// </summary>
        public TransformSequence<T> Then() => this with
        {
            currentTime = endTime
        };

        /// <summary>
        /// Continues with a delay from the current time.
        /// </summary>
        /// <param name="delay">The amount of time to delay.</param>
        public TransformSequence<T> Delay(double delay) => this with
        {
            currentTime = currentTime + delay
        };

        /// <summary>
        /// Creates an empty branch from the current time.
        /// </summary>
        public TransformSequenceBranch<T> CreateBranch() => new TransformSequenceBranch<T>(this with
        {
            startTime = currentTime,
            currentTime = currentTime,
            endTime = currentTime,
            length = 0
        });

        /// <summary>
        /// Continues with the result of merging a branch into the current sequence.
        /// </summary>
        /// <param name="branch">The branch to merge.</param>
        public TransformSequence<T> MergedWith(TransformSequenceBranch<T> branch) => this with
        {
            endTime = Math.Max(endTime, branch.Head.endTime),
            length = length + branch.Head.length,
            transform = branch.Head.transform
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
            if (length == 0)
                return this;

            Transform[] transformPool = ArrayPool<Transform>.Shared.Rent(length);
            Span<Transform> transforms = transformPool[..length];

            Transform it = transform!;

            for (int i = length - 1; i >= 0; i--)
            {
                transforms[i] = it;
                it = it.PreviousInSequence;
            }

            double iterDuration = endTime - startTime + pause;

            foreach (var t in transforms)
            {
                target.RemoveTransformNoAbort(t);

                // Update start and end times such that no transformations need to be instantly
                // looped right after they're added. This is required so that transforms can be
                // inserted in the correct order such that none of them trigger abortions on
                // each other due to instant re-sorting upon adding.
                double currentTransformTime = target.Time.Current;

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

                target.AddTransform(t);
            }

            ArrayPool<Transform>.Shared.Return(transformPool);
            return this;
        }

        public TransformSequence<T> Finally(Action<T> function)
        {
            OnComplete(function);
            OnAbort(function);
            return this;
        }

        public TransformSequence<T> OnComplete(Action<T> function)
        {
            T t = target;
            getOrCreateEventHandler().OnComplete += () => function(t);
            return this;
        }

        public TransformSequence<T> OnAbort(Action<T> function)
        {
            T t = target;
            getOrCreateEventHandler().OnAbort += () => function(t);
            return this;
        }

        private TransformSequenceEventHandler getOrCreateEventHandler()
        {
            if (target.GetTransformEventHandler(id) is not TransformSequenceEventHandler handler)
                target.AddTransform(handler = new TransformSequenceEventHandler(target, id));
            return handler;
        }

        /// <summary>
        /// A delegate that generates a new <see cref="TransformSequence{T}"/> on a given <paramref name="origin"/>.
        /// </summary>
        /// <param name="origin">The origin to generate a <see cref="TransformSequence{T}"/> for.</param>
        /// <returns>The generated <see cref="TransformSequence{T}"/>.</returns>
        public delegate TransformSequence<T> Generator(T origin);
    }

    public ref struct TransformSequenceBranch<T>(TransformSequence<T> head)
        where T : class, ITransformable
    {
        public TransformSequence<T> Head { get; private set; } = head;

        public void Commit(TransformSequence<T> head) => Head = head;
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
