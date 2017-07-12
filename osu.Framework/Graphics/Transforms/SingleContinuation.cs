// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Transforms
{
    class SingleContinuation<T> : TransformContinuation
        where T : Transformable<T>
    {
        public SingleContinuation(ITransform<T> transform)
        {
            transform.OnComplete = OnComplete;
            transform.OnAbort = OnAbort;
        }
    }
}
