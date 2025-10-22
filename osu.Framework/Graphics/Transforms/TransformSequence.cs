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
    public readonly ref struct TransformSequence<T>
        where T : class, ITransformable
    {
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
        /// Whether any transforms have been added to the sequence.
        /// </summary>
        [MemberNotNullWhen(false, nameof(transform))]
        public bool IsEmpty => length == 0;

        /// <summary>
        /// Creates a blank transform sequence.
        /// </summary>
        /// <param name="target">The transform target.</param>
        public static TransformSequence<T> Create(T target)
        {
            // Consume the global context, if any.
            if (TransformSequenceHelpers.ConsumeContext<T>() is Context context)
                return context.Restore();

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
            if (transform.Target is null)
                throwTargetIsNull();

            if (transform.Target is not T target)
                throwTargetTypeMismatch(typeof(T), transform.Target.GetType());

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

            [DoesNotReturn]
            static void throwTargetIsNull()
                => throw new ArgumentException("Transform target cannot be null.");

            [DoesNotReturn]
            static void throwTargetTypeMismatch(Type expected, Type actual)
                => throw new ArgumentException($"Transform target was expected to be of type '{expected.ReadableName()}' but was '{actual.ReadableName()}'.");
        }

        /// <summary>
        /// Continues with an action on the target.
        /// </summary>
        [MustUseReturnValue]
        public TransformSequence<T> Next(Generator generator)
            => generator(Next());

        /// <summary>
        /// Continues with an action on the target.
        /// </summary>
        [MustUseReturnValue]
        public T Next()
            => Next(out _);

        /// <summary>
        /// Continues with an action on the target.
        /// </summary>
        [MustUseReturnValue]
        public T Next(out T continuationTarget)
        {
            TransformSequenceHelpers.SaveContext(new Context(this));
            continuationTarget = target;
            return continuationTarget;
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
        /// Repeats all added transforms indefinitely.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        public TransformSequence<T> Loop(double pause = 0)
            => Loop(pause, -1);

        /// <summary>
        /// Repeats all added transforms.
        /// </summary>
        /// <param name="pause">The pause between iterations in milliseconds.</param>
        /// <param name="numIters">The number of iterations.</param>
        public TransformSequence<T> Loop(double pause, int numIters)
        {
            if (IsEmpty)
                return this;

            Transform[] transformPool = ArrayPool<Transform>.Shared.Rent(length);
            Span<Transform> transforms = transformPool[..length];

            Transform it = transform;

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

        /// <summary>
        /// Creates an empty branch from the current time.
        /// </summary>
        public Branch CreateBranch() => new Branch(this with
        {
            startTime = currentTime,
            currentTime = currentTime,
            endTime = currentTime,
            length = 0
        });

        public void Finally(Action<T> function)
        {
            OnComplete(function);
            OnAbort(function);
        }

        public void OnComplete(Action<T> function)
        {
            T t = target;
            getOrCreateEventHandler().OnComplete += () => function(t);
        }

        public void OnAbort(Action<T> function)
        {
            T t = target;
            getOrCreateEventHandler().OnAbort += () => function(t);
        }

        private TransformSequenceEventHandler getOrCreateEventHandler()
        {
            if (target.GetTransformEventHandler(id) is not TransformSequenceEventHandler handler)
                target.AddTransform(handler = new TransformSequenceEventHandler(target, id));
            return handler;
        }

        public ref struct Branch
        {
            /// <summary>
            /// The current head.
            /// </summary>
            public TransformSequence<T> Head { get; private set; }

            /// <summary>
            /// The sequence which this branch is based off.
            /// </summary>
            private readonly TransformSequence<T> root;

            internal Branch(TransformSequence<T> root)
            {
                this.root = root;
                Head = root;
            }

            /// <summary>
            /// Appends a commit.
            /// </summary>
            /// <param name="head">The new head.</param>
            public void Commit(TransformSequence<T> head)
                => Head = head;

            /// <summary>
            /// Continues with the result of merging this branch into the original sequence.
            /// </summary>
            public TransformSequence<T> Merge() => root with
            {
                endTime = Math.Max(root.endTime, Head.endTime),
                length = root.length + Head.length,
                transform = Head.transform
            };
        }

        public readonly struct Context(TransformSequence<T> sequence)
        {
            private readonly ulong id = sequence.id;
            private readonly T target = sequence.target;
            private readonly double startTime = sequence.startTime;
            private readonly double currentTime = sequence.currentTime;
            private readonly double endTime = sequence.endTime;
            private readonly int length = sequence.length;
            private readonly Transform? transform = sequence.transform;

            public TransformSequence<T> Restore() => new TransformSequence<T>
            {
                id = id,
                target = target,
                startTime = startTime,
                currentTime = currentTime,
                endTime = endTime,
                length = length,
                transform = transform
            };
        }

        /// <summary>
        /// A delegate that generates a new <see cref="TransformSequence{T}"/> on a given <paramref name="origin"/>.
        /// </summary>
        /// <param name="origin">The origin to generate a <see cref="TransformSequence{T}"/> for.</param>
        /// <returns>The generated <see cref="TransformSequence{T}"/>.</returns>
        public delegate TransformSequence<T> Generator(T origin);
    }

    internal static class TransformSequenceHelpers
    {
        private static ulong id = 1;

        public static ulong GetNextId()
            => Interlocked.Increment(ref id);

        public static void SaveContext<T>(TransformSequence<T>.Context context)
            where T : class, ITransformable
            => Context<T>.Current = context;

        public static TransformSequence<T>.Context? ConsumeContext<T>()
            where T : class, ITransformable
        {
            TransformSequence<T>.Context? context = Context<T>.Current;
            Context<T>.Current = null;
            return context;
        }

        private static class Context<T>
            where T : class, ITransformable
        {
            [ThreadStatic]
            public static TransformSequence<T>.Context? Current;
        }
    }
}
