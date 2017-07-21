// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformSequence<T>
        where T : ITransformable
    {
        public delegate TransformSequence<T> Generator(T newSequence);

        private readonly T origin;

        private readonly List<Transform> transforms = new List<Transform>();

        private bool hasCompleted;

        private readonly double startTime;
        private double currentTime;
        private double endTime => Math.Max(currentTime, lastEndTime);

        private Transform last;
        private double lastEndTime;

        private bool hasEnd => lastEndTime != double.PositiveInfinity;

        public TransformSequence(T origin)
        {
            if (origin == null)
                throw new NullReferenceException($"May not create a {nameof(TransformSequence<T>)} with a null {nameof(origin)}.");

            this.origin = origin;
            startTime = currentTime = lastEndTime = origin.TransformStartTime;
        }

        private void onLoopingTransform()
        {
            // As soon as we have an infinitely looping transform,
            // completion no longer makes sense.
            if (last != null)
                last.OnComplete = null;

            last = null;
            lastEndTime = double.PositiveInfinity;
            hasCompleted = false;
        }

        public TransformSequence<T> Append(Transform transform)
        {
            transforms.Add(transform);

            transform.OnComplete = null;
            transform.OnAbort = onTransformAborted;

            if (transform.IsLooping)
                onLoopingTransform();

            // Update last transform for completion callback
            if (last == null || transform.EndTime > lastEndTime)
            {
                if (last != null)
                    last.OnComplete = null;

                last = transform;
                last.OnComplete = onTransformsComplete;
                lastEndTime = last.EndTime;
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
            using (origin.BeginAbsoluteSequence(currentTime))
                child = childGenerator(origin);

            if (!ReferenceEquals(child.origin, origin))
                throw new InvalidOperationException($"May not append {nameof(TransformSequence<T>)} with different origin.");

            var oldLast = last;
            foreach (var t in child.transforms)
                Append(t);

            // If we flatten a child into ourselves that already completed, then
            // we need to make sure to update the hasCompleted value, too, since
            // the already completed final transform will no longer fire any events.
            if (oldLast != last)
                hasCompleted = child.hasCompleted;

            return this;
        }

        internal TransformSequence<T> Append<U>(Func<T, U> originFunc, out U result)
        {
            using (origin.BeginAbsoluteSequence(currentTime))
                result = originFunc(origin);

            return this;
        }

        internal TransformSequence<T> Append(Action<T> originAction)
        {
            using (origin.BeginAbsoluteSequence(currentTime))
                originAction(origin);

            return this;
        }

        private void onTransformAborted()
        {
            if (transforms.Count == 0)
                return;

            // No need for OnAbort events to trigger anymore, since
            // we are already aware of the abortion.
            foreach (var t in transforms)
            {
                t.OnAbort = null;
                t.TargetTransformable.RemoveTransform(t);
            }

            transforms.Clear();
            last = null;

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
                func();
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

        public TransformSequence<T> Loop(double pause, int numIters, params Generator[] childGenerators)
        {
            Append(o =>
            {
                var childSequence = new TransformSequence<T>(o);
                childSequence.Append(childGenerators);
                childSequence.Loop(pause, numIters);
                return childSequence;
            });

            return this;
        }

        public TransformSequence<T> Loop(double pause, int numIters)
        {
            if (numIters < 1)
                throw new InvalidOperationException($"May not {nameof(Loop)} for fewer than 1 iteration ({numIters} attempted).");

            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Loop)} on an endless {nameof(TransformSequence<T>)}.");

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
                    t.TargetTransformable.AddTransform(clone);
                }
            }

            return this;
        }

        public TransformSequence<T> Loop(params Generator[] childGenerators) => Loop(0, childGenerators);

        public TransformSequence<T> Loop(double pause, params Generator[] childGenerators)
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

        public TransformSequence<T> Loop(double pause = 0)
        {
            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Loop)} on an endless {nameof(TransformSequence<T>)}.");

            var iterDuration = endTime - startTime + pause;
            foreach (var t in transforms)
            {
                t.IsLooping = true;
                t.LoopDelay = iterDuration;
            }

            onLoopingTransform();
            return this;
        }

        public TransformSequence<T> Then(params Generator[] childGenerators) => Then(0, childGenerators);

        public TransformSequence<T> Then(double delay, params Generator[] childGenerators)
        {
            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Then)} on an endless {nameof(TransformSequence<T>)}.");

            // "Then" simply sets the currentTime to endTime to continue where the last transform left off,
            // followed by a subsequent delay call.
            currentTime = endTime;
            return Delay(delay, childGenerators);
        }

        public TransformSequence<T> Delay(double delay, params Generator[] childGenerators)
        {
            // After a delay statement, future transforms are appended after a currentTime which got offset by a delay.
            currentTime += delay;
            return Append(childGenerators);
        }

        public void OnComplete(Action<T> func)
        {
            if (!hasEnd)
                throw new InvalidOperationException($"Can not perform {nameof(Then)} on an endless {nameof(TransformSequence<T>)}.");

            subscribeComplete(() => func(origin));
        }

        public void OnAbort(Action<T> func) => subscribeAbort(() => func(origin));

        public void Finally(Action<T> func)
        {
            if (hasEnd)
                OnComplete(func);
            OnAbort(func);
        }
    }
}
