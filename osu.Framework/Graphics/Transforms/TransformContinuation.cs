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

        private int countPreconditions;
        private readonly List<Func<T, TransformContinuation<T>>> preconditions = new List<Func<T, TransformContinuation<T>>>();

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

        internal void Invoke()
        {
            if (state != State.Dormant)
                throw new InvalidOperationException($"Cannot invoke the same continuation twice.");

            state = State.Running;

            foreach (var p in preconditions)
                continuePrecondition(p);
        }

        private void continuePrecondition(Func<T, TransformContinuation<T>> precondition)
        {
            Trace.Assert(state == State.Running);

            var continuation = precondition.Invoke(origin);
            if (!ReferenceEquals(continuation.origin, origin))
                throw new InvalidOperationException($"May only chain transforms on the same origin, but chained {continuation.origin} from {origin}.");

            continuation.SubscribeCompleted(onPreconditionComplete);
            continuation.SubscribeAborted(onPreconditionAbort);
        }

        public TransformContinuation<T> AddTransformPrecondition(ITransform transform)
        {
            if (state == State.Dormant)
                Invoke();

            ++countPreconditions;

            transform.OnComplete = onPreconditionComplete;
            transform.OnAbort = onPreconditionAbort;

            return this;
        }

        public TransformContinuation<T> AddPreconditions(IEnumerable<Func<T, TransformContinuation<T>>> preconditions)
        {
            foreach (var p in preconditions)
                AddPrecondition(p);

            return this;
        }

        public TransformContinuation<T> AddPrecondition(Func<T, TransformContinuation<T>> precondition)
        {
            if (state == State.Finished)
                throw new InvalidOperationException($"Cannot add preconditions to finished continuations.");

            ++countPreconditions;

            if (state == State.Running)
                continuePrecondition(precondition);
            else
                preconditions.Add(precondition);

            return this;
        }

        private int countPreconditionsComplete;
        private void onPreconditionComplete(double offset)
        {
            ++countPreconditionsComplete;
            if (countPreconditionsComplete == countPreconditions)
                OnComplete(offset);
        }

        private int countPreconditionsAborted;
        private void onPreconditionAbort(double offset)
        {
            if (countPreconditionsAborted == 0)
                OnAbort(offset);
            ++countPreconditionsAborted;
        }

        private event Action<double> onCompleteActions;
        private event Action<double> onAbortActions;

        public void SubscribeCompleted(Action<double> action) => onCompleteActions += action;

        public void SubscribeAborted(Action<double> action) => onAbortActions += action;

        protected void OnComplete(double offset) => onCompleteActions?.Invoke(offset);

        protected void OnAbort(double offset) => onAbortActions?.Invoke(offset);

        public TransformContinuation<T> Loop(double pause = 0, int numIters = -1)
        {
            return null;
        }

        private void onTrigger(double offset, IEnumerable<Func<T, TransformContinuation<T>>> funcs)
        {
            origin.WithDelay(-offset, delegate
            {
                AddPreconditions(funcs);
                Invoke();
            });
        }

        private TransformContinuation<T> then(IEnumerable<Func<T, TransformContinuation<T>>> funcs)
        {
            var result = new TransformContinuation<T>(origin);
            onCompleteActions += offset => result.onTrigger(offset, funcs);
            return result;
        }

        public TransformContinuation<T> Then(Func<T, TransformContinuation<T>> firstFun, params Func<T, TransformContinuation<T>>[] funcs) =>
            then(new[] { firstFun }.Concat(funcs));

        public TransformContinuation<T> WaitForCompletion() => then(new Func<T, TransformContinuation<T>>[0]);

        public void Then(Action<double> func) => onCompleteActions += func;

        public void OnAbort(Action<double> func) => onAbortActions += func;

        public void Finally(Action<double> func)
        {
            Then(func);
            OnAbort(func);
        }
    }
}
