// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformContinuation<T> : ITransformContinuation<T>
        where T : Transformable
    {
        public T Origin { get; }

        private int countPreconditions;
        private readonly List<Func<ITransformContinuation<T>>> preconditions = new List<Func<ITransformContinuation<T>>>();

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

        private void continuePrecondition(Func<ITransformContinuation<T>> precondition)
        {
            Trace.Assert(state == State.Running);

            var continuation = precondition.Invoke();
            if (continuation.Origin != Origin)
                throw new InvalidOperationException($"May only chain transforms on the same origin, but chained {continuation.Origin} from {Origin}.");

            continuation.SubscribeCompleted(onPreconditionComplete);
            continuation.SubscribeAborted(onPreconditionAbort);
        }

        public ITransformContinuation<T> AddTransformPrecondition(ITransform transform)
        {
            if (state == State.Dormant)
                Invoke();

            ++countPreconditions;

            transform.OnComplete = onPreconditionComplete;
            transform.OnAbort = onPreconditionAbort;

            return this;
        }

        public ITransformContinuation<T> AddPreconditions(IEnumerable<Func<ITransformContinuation<T>>> preconditions)
        {
            foreach (var p in preconditions)
                AddPrecondition(p);

            return this;
        }

        public ITransformContinuation<T> AddPrecondition(Func<ITransformContinuation<T>> precondition)
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

        private void onTrigger(double offset, IEnumerable<Func<ITransformContinuation<T>>> funcs)
        {
            Origin.Delay(-offset);
            AddPreconditions(funcs);
            Invoke();
            Origin.Delay(offset);
        }

        private ITransformContinuation<T> then(IEnumerable<Func<ITransformContinuation<T>>> funcs)
        {
            var result = new TransformContinuation<T>(Origin);
            onCompleteActions += offset => result.onTrigger(offset, funcs);
            return result;
        }

        public ITransformContinuation<T> Then(Func<ITransformContinuation<T>> firstFun, params Func<ITransformContinuation<T>>[] funcs) =>
            then(new[] { firstFun }.Concat(funcs));

        private Func<ITransformContinuation<T>> bindOrigin(Func<T, ITransformContinuation<T>> fun) => () => fun(Origin);

        public ITransformContinuation<T> Then(Func<T, ITransformContinuation<T>> firstFun, params Func<T, ITransformContinuation<T>>[] funcs) =>
            then(new[] { firstFun }.Concat(funcs).Select(bindOrigin));

        public ITransformContinuation<T> WaitForCompletion() => then(new Func<ITransformContinuation<T>>[0]);

        public void Then(Action<double> func) => onCompleteActions += func;

        public void OnAbort(Action<double> func) => onAbortActions += func;

        public void Finally(Action<double> func)
        {
            Then(func);
            OnAbort(func);
        }
    }
}
