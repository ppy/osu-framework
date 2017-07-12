// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    abstract public class TransformContinuation
    {
        protected TransformContinuation()
        {
        }

        public event Action OnCompleteActions;
        public event Action OnAbortActions;

        protected void OnComplete() => OnCompleteActions?.Invoke();

        protected void OnAbort() => OnAbortActions?.Invoke();

        public TransformContinuation Then(params Func<TransformContinuation>[] funcs)
        {
            var result = new CompositeContinuation(funcs);
            OnCompleteActions += () => result.Invoke();
            return result;
        }

        public TransformContinuation OnAbort(params Func<TransformContinuation>[] funcs)
        {
            var result = new CompositeContinuation(funcs);
            OnAbortActions += () => result.Invoke();
            return result;
        }

        public TransformContinuation Finally(params Func<TransformContinuation>[] funcs)
        {
            var result = new CompositeContinuation(funcs);
            OnCompleteActions += () => result.Invoke();
            OnAbortActions += () => result.Invoke();
            return result;
        }

        public void Then(Action func) => OnCompleteActions += func;

        public void OnAbort(Action func) => OnAbortActions += func;

        public void Finally(Action func)
        {
            Then(func);
            OnAbort(func);
        }
    }
}
