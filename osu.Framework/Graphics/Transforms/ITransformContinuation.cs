// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransformContinuation<T>
    {
        T Origin { get; }

        ITransformContinuation<T> AddPreconditions(IEnumerable<Func<ITransformContinuation<T>>> preconditions);

        ITransformContinuation<T> AddPrecondition(Func<ITransformContinuation<T>> precondition);

        ITransformContinuation<T> Then(Func<ITransformContinuation<T>> firstFun, params Func<ITransformContinuation<T>>[] funcs);

        ITransformContinuation<T> Then(Func<T, ITransformContinuation<T>> firstFun, params Func<T, ITransformContinuation<T>>[] funcs);

        ITransformContinuation<T> WaitForCompletion();

        void SubscribeCompleted(Action<double> action);

        void SubscribeAborted(Action<double> action);
    }
}
