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

        // countChildren may be different from the amount of children generators, as we can have
        // a simple transform as child via our second constructor.
        private int countChildren;
        private readonly List<Func<T, TransformContinuation<T>>> childrenGenerators = new List<Func<T, TransformContinuation<T>>>();

        private enum State
        {
            Dormant,
            Running,
            Finished,
        }

        private State state = State.Dormant;

        public TransformContinuation(T origin)
        {
            this.origin = origin;
        }

        public TransformContinuation(T origin, ITransform transform) : this(origin)
        {
            run(0);

            ++countChildren;

            transform.OnComplete = onChildComplete;
            transform.OnAbort = onChildAbort;
        }

        private void run(double offset)
        {
            origin.WithDelay(-offset, delegate
            {
                if (state != State.Dormant)
                    throw new InvalidOperationException($"Cannot invoke the same continuation twice.");

                state = State.Running;

                foreach (var p in childrenGenerators)
                    generateChild(p);
            });
        }

        private void generateChild(Func<T, TransformContinuation<T>> childGenerator)
        {
            Trace.Assert(state == State.Running);

            var childContinuation = childGenerator.Invoke(origin);
            if (!ReferenceEquals(childContinuation.origin, origin))
                throw new InvalidOperationException($"May only chain transforms on the same origin, but chained {childContinuation.origin} from {origin}.");

            childContinuation.onAllChildrenComplete += onChildComplete;
            childContinuation.onOneChildAbort += onChildAbort;
        }

        public TransformContinuation<T> AddChildGenerators(IEnumerable<Func<T, TransformContinuation<T>>> childGenerators)
        {
            foreach (var p in childGenerators)
                AddChildGenerator(p);

            return this;
        }

        public TransformContinuation<T> AddChildGenerator(Func<T, TransformContinuation<T>> childGenerator)
        {
            if (state == State.Finished)
                throw new InvalidOperationException($"Cannot add preconditions to finished continuations.");

            ++countChildren;

            childrenGenerators.Add(childGenerator);
            if (state == State.Running)
                generateChild(childGenerator);

            return this;
        }

        private int countChildrenComplete;
        private void onChildComplete(double offset)
        {
            ++countChildrenComplete;
            if (countChildrenComplete == countChildren)
                onAllChildrenComplete?.Invoke(offset);
        }

        private int countChildrenAborted;
        private void onChildAbort(double offset)
        {
            if (countChildrenAborted == 0)
                onOneChildAbort?.Invoke(offset);
            ++countChildrenAborted;
        }

        private event Action<double> onAllChildrenComplete;
        private event Action<double> onOneChildAbort;

        public TransformContinuation<T> Loop(double pause = 0, int numIters = -1)
        {
            return null;
        }

        private TransformContinuation<T> then(IEnumerable<Func<T, TransformContinuation<T>>> childGenerators)
        {
            var result = new TransformContinuation<T>(origin);
            result.AddChildGenerators(childGenerators);
            onAllChildrenComplete += offset => result.run(offset);
            return result;
        }

        public TransformContinuation<T> Then(params Func<T, TransformContinuation<T>>[] childGenerators) => then(childGenerators);

        public TransformContinuation<T> WaitForCompletion() => then(new Func<T, TransformContinuation<T>>[0]);

        public void Then(Action<double> func) => onAllChildrenComplete += func;

        public void OnAbort(Action<double> func) => onOneChildAbort += func;

        public void Finally(Action<double> func)
        {
            Then(func);
            OnAbort(func);
        }
    }
}
