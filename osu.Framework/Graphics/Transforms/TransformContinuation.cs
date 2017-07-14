// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformContinuation<T>
        where T : ITransformable
    {
        private T origin { get; }

        private readonly double delay;
        private readonly List<Func<T, TransformContinuation<T>>> childrenGenerators = new List<Func<T, TransformContinuation<T>>>();

        // countChildren may be different from the amount of children generators, as we can have
        // a simple transform as child via our second constructor.
        private int countChildren;
        private int countChildrenComplete;
        private int countChildrenAborted;

        private double finishedOffset;

        // Amount of additional loop iterations to perform
        private long remainingIterations;
        // Pause in milliseconds between iterations
        private double iterationPause;

        private enum State
        {
            Dormant,
            Running,
            Finished,
        }

        private State state = State.Dormant;

        public TransformContinuation(T origin, bool running, double delay)
        {
            this.delay = delay;
            this.origin = origin;
            if (running)
                run(0);
        }

        public TransformContinuation(T origin, ITransform transform) : this(origin, true, 0)
        {
            ++countChildren;

            // If we transform already finished, then let's immediately trigger a completion
            if (transform.EndTime <= transform.Time?.Current)
                onChildComplete(0);
            else
            {
                transform.OnComplete = onChildComplete;
                transform.OnAbort = onChildAbort;
            }
        }

        private void run(double offset)
        {
            if (state == State.Running)
                throw new InvalidOperationException("Cannot invoke the same continuation twice.");

            state = State.Running;

            countChildrenComplete = 0;
            countChildrenAborted = 0;

            TransformContinuation<T>[] children;

            using (origin.BeginDelayedSequence(-offset))
                children = childrenGenerators.Select(generateChild).ToArray();

            foreach (var c in children)
            {
                c.subscribeComplete(onChildComplete);
                c.subscribeAbort(onChildAbort);
            }
        }

        private TransformContinuation<T> generateChild(Func<T, TransformContinuation<T>> childGenerator)
        {
            var childContinuation = childGenerator.Invoke(origin);
            if (!ReferenceEquals(childContinuation.origin, origin))
                throw new InvalidOperationException($"May only chain transforms on the same origin, but chained {childContinuation.origin} from {origin}.");

            return childContinuation;
        }

        public TransformContinuation<T> AddChildGenerators(IEnumerable<Func<T, TransformContinuation<T>>> childGenerators)
        {
            foreach (var p in childGenerators)
                AddChildGenerator(p);

            return this;
        }

        public TransformContinuation<T> AddChildGenerator(Func<T, TransformContinuation<T>> childGenerator)
        {
            ++countChildren;

            childrenGenerators.Add(childGenerator);

            if (state != State.Dormant)
            {
                state = State.Running;

                TransformContinuation<T> c;
                using (origin.BeginDelayedSequence(delay))
                    c = generateChild(childGenerator);

                c.subscribeComplete(onChildComplete);
                c.subscribeAbort(onChildAbort);
            }

            return this;
        }

        private void onChildComplete(double offset)
        {
            ++countChildrenComplete;
            if (countChildrenComplete == countChildren)
                triggerComplete(offset);
        }

        private void onChildAbort(double offset)
        {
            if (countChildrenAborted++ == 0)
                triggerAbort(offset);
        }

        private void triggerComplete(double offset)
        {
            state = State.Finished;
            if (remainingIterations == 0)
            {
                finishedOffset = offset;
                OnComplete?.Invoke(offset);
                return;
            }

            --remainingIterations;
            run(offset - iterationPause);
        }

        private void triggerAbort(double offset)
        {
            state = State.Finished;

            finishedOffset = offset;
            OnAbort?.Invoke(offset);
        }

        private void subscribeComplete(Action<double> action)
        {
            if (state != State.Finished)
                OnComplete += action;
            // If we were already completed before, immediately trigger.
            else if (countChildrenComplete == countChildren)
                action.Invoke(finishedOffset);
        }

        private void subscribeAbort(Action<double> action)
        {
            if (state != State.Finished)
                OnAbort += action;
            // If we were already aborted before, immediately trigger.
            else if(countChildrenAborted > 0)
                action.Invoke(finishedOffset);
        }

        private event Action<double> OnComplete;
        private event Action<double> OnAbort;

        private TransformContinuation<T> loop(double pause, int numIters, IEnumerable<Func<T, TransformContinuation<T>>> childGenerators)
        {
            // Add a new child generator, which will generate an immediately running and looping continuation.
            AddChildGenerator(o =>
            {
                var loopedContinuation = new TransformContinuation<T>(o, false, 0)
                {
                    remainingIterations = numIters - 1,
                    iterationPause = pause,
                };

                loopedContinuation.AddChildGenerators(childGenerators);
                loopedContinuation.run(0);

                return loopedContinuation;
            });

            return this;
        }

        public TransformContinuation<T> Loop(double pause, int numIters, Func<T, TransformContinuation<T>> firstChildGenerator, params Func<T, TransformContinuation<T>>[] childGenerators) =>
            loop(pause, numIters, new[] { firstChildGenerator }.Concat(childGenerators));

        public TransformContinuation<T> Loop(double pause, Func<T, TransformContinuation<T>> firstChildGenerator, params Func<T, TransformContinuation<T>>[] childGenerators) =>
            Loop(pause, -1, firstChildGenerator, childGenerators);

        public TransformContinuation<T> Loop(Func<T, TransformContinuation<T>> firstChildGenerator, params Func<T, TransformContinuation<T>>[] childGenerators) =>
            Loop(0, -1, firstChildGenerator, childGenerators);

        private TransformContinuation<T> then(double delay, IEnumerable<Func<T, TransformContinuation<T>>> childGenerators)
        {
            var nextContinuation = new TransformContinuation<T>(origin, false, delay);
            nextContinuation.AddChildGenerators(childGenerators);
            subscribeComplete(offset => nextContinuation.run(offset - delay));
            return nextContinuation;
        }

        public TransformContinuation<T> Then(double delay, params Func<T, TransformContinuation<T>>[] childGenerators) => then(delay, childGenerators);

        public TransformContinuation<T> Then(params Func<T, TransformContinuation<T>>[] childGenerators) => then(0, childGenerators);

        public TransformContinuation<T> WaitForCompletion(double delay = 0) => then(delay, new Func<T, TransformContinuation<T>>[0]);

        public void Then(Action<double> func) => subscribeComplete(func);

        public void WhenAborted(Action<double> func)
        {
            throw new NotImplementedException();
        }

        public void Finally(Action<double> func)
        {
            Then(func);
            WhenAborted(func);
        }
    }
}
