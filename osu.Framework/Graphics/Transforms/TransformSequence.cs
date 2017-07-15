// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformSequence<T>
        where T : ITransformable
    {
        public delegate TransformSequence<T> Generator(T newSequence);

        private readonly T origin;

        private readonly List<Transform> transforms = new List<Transform>();
        private Transform last;

        private bool hasCompleted;

        private readonly double startTime;
        private double currentTime;
        private double endTime;

        public TransformSequence(T origin)
        {
            if (origin == null)
                throw new NullReferenceException($"May not create a {nameof(TransformSequence<T>)} with a null {nameof(origin)}.");

            this.origin = origin;
            startTime = currentTime = endTime = origin.TransformStartTime;
        }

        public TransformSequence<T> Append(Transform transform)
        {
            transforms.Add(transform);

            transform.OnComplete = null;
            transform.OnAbort = onTransformAborted;

            // Update last transform for completion callback
            if (transform.EndTime > endTime)
            {
                if (last != null)
                    last.OnComplete = null;

                last = transform;
                last.OnComplete = onTransformsComplete;
                endTime = last.EndTime;
                hasCompleted = false;
            }

            return this;
        }

        public TransformSequence<T> Append(IEnumerable<Generator> childGenerators)
        {
            foreach (var p in childGenerators)
                Append(p);

            return this;
        }

        public TransformSequence<T> Append(Generator childGenerator)
        {
            TransformSequence<T> child;
            using (origin.BeginDelayedSequence(currentTime - startTime))
                child = childGenerator.Invoke(origin);

            foreach (var t in child.transforms)
                Append(t);

            return this;
        }

        private void onTransformAborted()
        {
            if (transforms.Count == 0)
                return;

            origin.RemoveTransforms(transforms);
            transforms.Clear();

            onAbort?.Invoke();
        }

        private void onTransformsComplete()
        {
            hasCompleted = true;
            onComplete?.Invoke();
        }

        private void subscribeComplete(Action func)
        {
            if (onComplete != null)
                throw new InvalidOperationException(
                    "May not subscribe completion multiple times." +
                    $"This exception is also caused by calling {nameof(Then)} or {nameof(Finally)} on an infinitely looping {nameof(TransformSequence<T>)}.");

            onComplete = func;

            // Completion can be immediately triggered by instant transforms,
            // and therefore when subscribing we need to take into account
            // potential previous completions.
            if (hasCompleted)
                func.Invoke();
        }

        private void subscribeAbort(Action func)
        {
            if (onAbort != null)
                throw new InvalidOperationException("May not subscribe abort multiple times.");

            // No need to worry about new transforms immediately aborting, so
            // we can just subscribe here and be sure abort couldn't have been
            // triggered already.
            onAbort = func;
        }

        private Action onComplete;
        private Action onAbort;

        public TransformSequence<T> Loop(double pause = 0)
        {
            var iterDuration = endTime - startTime + pause;
            foreach (var t in transforms)
            {
                t.IsLooping = true;
                t.LoopDelay = iterDuration;
            }

            return this;
        }

        public TransformSequence<T> Loop(int numIters, double pause = 0)
        {
            if (numIters < 1)
                throw new InvalidOperationException($"May not loop for fewer than 1 iteration ({numIters} attempted).");

            var iterDuration = endTime - startTime + pause;
            var toLoop = transforms.ToArray();

            // Duplicate existing transforms numIters times
            for (int i = 1; i < numIters; ++i)
            {
                foreach (var t in toLoop)
                {
                    var clone = t.Clone();
                    clone.StartTime += i * iterDuration;
                    clone.EndTime += i * iterDuration;
                    Append(clone);
                    origin.AddTransform(clone);
                }
            }

            return this;
        }

        private TransformSequence<T> loop(int numIters, double pause, IEnumerable<Generator> childGenerators)
        {
            Append(o =>
            {
                var childSequence = new TransformSequence<T>(o);
                childSequence.Append(childGenerators);
                childSequence.Loop(numIters, pause);
                return childSequence;
            });

            return this;
        }

        private TransformSequence<T> loop(double pause, IEnumerable<Generator> childGenerators)
        {
            Append(o =>
            {
                var childSequence = new TransformSequence<T>(o);
                childSequence.Append(childGenerators);
                childSequence.Loop(pause);
                return childSequence;
            });

            return this;
        }

        public TransformSequence<T> Loop(int numIters, double pause, Generator firstChildGenerator, params Generator[] childGenerators) =>
            loop(numIters, pause, new[] { firstChildGenerator }.Concat(childGenerators));

        public TransformSequence<T> Loop(double pause, Generator firstChildGenerator, params Generator[] childGenerators) =>
            loop(pause, new[] { firstChildGenerator }.Concat(childGenerators));

        public TransformSequence<T> Loop(Generator firstChildGenerator, params Generator[] childGenerators) =>
            Loop(0, firstChildGenerator, childGenerators);

        private TransformSequence<T> then(double nextDelay, IEnumerable<Generator> nextChildGenerators)
        {
            // After a then statement, future transforms are appended after our last one finished
            // plus the specified extra delay.
            endTime += nextDelay;
            currentTime = endTime;
            return Append(nextChildGenerators);
        }

        public TransformSequence<T> Then(double delay, params Generator[] childGenerators) => then(delay, childGenerators);

        public TransformSequence<T> Then(params Generator[] childGenerators) => then(0, childGenerators);

        public void Then(Action<T> func) => subscribeComplete(() => func(origin));

        public void Catch(Action<T> func) => subscribeAbort(() => func(origin));

        public void Finally(Action<T> func)
        {
            Then(func);
            Catch(func);
        }
    }
}
