// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private bool isAborted;

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

        public TransformContinuation(T origin, ITransform transform) : this(origin, false, 0)
        {
            ++countChildren;
            run(0);

            // If we transform already finished, then let's immediately trigger a completion
            if (transform.EndTime <= transform.Time?.Current)
                onChildComplete(0);
            else
            {
                transform.OnComplete = onChildComplete;
                transform.OnAbort = offset => onChildAbort();
            }
        }

        private void run(double offset)
        {
            Trace.Assert(!isAborted, "Cannot run an aborted continuation.");

            if (state == State.Running)
                throw new InvalidOperationException("Cannot invoke the same continuation twice.");

            state = State.Running;

            countChildrenComplete = 0;

            if (childrenGenerators.Count > 0)
            {
                TransformContinuation<T>[] children;

                using (origin.BeginDelayedSequence(delay - offset))
                    children = childrenGenerators.Select(generateChild).ToArray();

                foreach (var c in children)
                {
                    c.subscribeComplete(onChildComplete);
                    c.subscribeAbort(onChildAbort);
                }
            }
            else if (countChildren == 0)
                triggerComplete(offset - delay);
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
                // If we already finished, then let's check how early / late we finished
                // to determine the offset of child start points, assuming that we are
                // adding the new child right when we finish.
                double actualDelay = state == State.Finished ? -finishedOffset : delay;

                state = State.Running;

                TransformContinuation<T> c;
                using (origin.BeginDelayedSequence(actualDelay))
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

        private void onChildAbort()
        {
            if (!isAborted)
                triggerAbort();
        }

        private void triggerComplete(double offset)
        {
            Trace.Assert(!isAborted, "It is impossible for a transform continuation to both complete and be aborted.");

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

        private void triggerAbort()
        {
            state = State.Finished;

            isAborted = true;
            OnAbort?.Invoke();
        }

        private void subscribeComplete(Action<double> action)
        {
            // If we are already completed, immediately trigger.
            if (countChildrenComplete == countChildren)
            {
                Trace.Assert(!isAborted, "It is impossible for a transform continuation to both complete and be aborted.");
                action.Invoke(finishedOffset);
            }
            else
                OnComplete += action;
        }

        private void subscribeAbort(Action action)
        {
            // If we were already aborted before, immediately trigger.
            if (isAborted)
                action.Invoke();
            else
                OnAbort += action;
        }

        private event Action<double> OnComplete;
        private event Action OnAbort;

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

        private TransformContinuation<T> then(double nextDelay, IEnumerable<Func<T, TransformContinuation<T>>> nextChildGenerators)
        {
            var nextContinuation = new TransformContinuation<T>(origin, false, nextDelay);
            nextContinuation.AddChildGenerators(nextChildGenerators);
            subscribeComplete(offset => nextContinuation.run(offset));
            subscribeAbort(() => nextContinuation.triggerAbort());

            return nextContinuation;
        }

        public TransformContinuation<T> Then(double delay, params Func<T, TransformContinuation<T>>[] childGenerators) => then(delay, childGenerators);

        public TransformContinuation<T> Then(params Func<T, TransformContinuation<T>>[] childGenerators) => then(0, childGenerators);

        public void Then(Action func) => subscribeComplete(offset => func());

        public void Catch(Action func) => subscribeAbort(func);

        public void Finally(Action func)
        {
            Then(func);
            Catch(func);
        }
    }
}
