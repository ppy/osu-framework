// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Transforms
{
    public class CompositeContinuation : TransformContinuation
    {
        private readonly Func<TransformContinuation>[] funcs;

        public CompositeContinuation(params Func<TransformContinuation>[] funcs)
        {
            this.funcs = funcs;
        }

        public void Invoke()
        {
            foreach (var continuation in funcs.Select(f => f.Invoke()))
            {
                continuation.OnCompleteActions += onCompleteCallback;
                continuation.OnAbortActions += onAbortCallback;
            }
        }


        private int countCompletions;
        private void onCompleteCallback()
        {
            ++countCompletions;
            if (countCompletions == funcs.Length)
                OnComplete();
        }

        private int countAbortions;
        private void onAbortCallback()
        {
            if (countAbortions == 0)
                OnAbort();
            ++countAbortions;
        }
    }
}
