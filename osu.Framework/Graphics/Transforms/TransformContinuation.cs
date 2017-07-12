// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformContinuation<T>
        where T : Transformable<T>
    {
        public readonly T Origin;

        private int countPreconditions;
        private readonly List<Func<TransformContinuation<T>>> preconditions = new List<Func<TransformContinuation<T>>>();

        private enum State
        {
            Dormant,
            Running,
            Finished,
        }

        private State state = State.Dormant;

        public TransformContinuation(T origin)
        {
            Origin = origin;
        }

        internal void Invoke()
        {
            if (state != State.Dormant)
                throw new InvalidOperationException($"Cannot invoke the same continuation twice.");

            state = State.Running;

            foreach (var p in preconditions)
                continuePrecondition(p);
        }

        private void continuePrecondition(Func<TransformContinuation<T>> precondition)
        {
            Trace.Assert(state == State.Running);

            var continuation = precondition.Invoke();
            if (continuation.Origin != Origin)
                throw new InvalidOperationException($"May only chain transforms on the same origin, but chained {continuation.Origin} from {Origin}.");

            continuation.OnCompleteActions += onPreconditionComplete;
            continuation.OnAbortActions += onPreconditionAbort;
        }

        public TransformContinuation<T> AddTransformPrecondition(ITransform<T> transform)
        {
            if (state == State.Dormant)
                Invoke();

            ++countPreconditions;

            transform.OnComplete = onPreconditionComplete;
            transform.OnAbort = onPreconditionAbort;

            return this;
        }

        public TransformContinuation<T> AddPreconditions(IEnumerable<Func<TransformContinuation<T>>> preconditions)
        {
            foreach (var p in preconditions)
                AddPrecondition(p);

            return this;
        }

        public TransformContinuation<T> AddPrecondition(Func<TransformContinuation<T>> precondition)
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

        public event Action<double> OnCompleteActions;
        public event Action<double> OnAbortActions;

        protected void OnComplete(double offset) => OnCompleteActions?.Invoke(offset);

        protected void OnAbort(double offset) => OnAbortActions?.Invoke(offset);

        public TransformContinuation<T> Loop(double pause = 0, int numIters = -1)
        {
            return null;
        }

        private void onTrigger(double offset, Func<TransformContinuation<T>>[] funcs)
        {
            Origin.Delay(-offset);
            AddPreconditions(funcs);
            Invoke();
            Origin.Delay(offset);
        }

        public TransformContinuation<T> Then(params Func<TransformContinuation<T>>[] funcs)
        {
            var result = new TransformContinuation<T>(Origin);
            OnCompleteActions += offset => result.onTrigger(offset, funcs);
            return result;
        }

        public TransformContinuation<T> OnAbort(params Func<TransformContinuation<T>>[] funcs)
        {
            var result = new TransformContinuation<T>(Origin);
            OnAbortActions += offset => result.onTrigger(offset, funcs);
            return result;
        }

        public TransformContinuation<T> Finally(params Func<TransformContinuation<T>>[] funcs)
        {
            var result = new TransformContinuation<T>(Origin);
            OnCompleteActions += offset => result.onTrigger(offset, funcs);
            OnAbortActions += offset => result.onTrigger(offset, funcs);
            return result;
        }

        public void Then(Action<double> func) => OnCompleteActions += func;

        public void OnAbort(Action<double> func) => OnAbortActions += func;

        public void Finally(Action<double> func)
        {
            Then(func);
            OnAbort(func);
        }
    }
}
