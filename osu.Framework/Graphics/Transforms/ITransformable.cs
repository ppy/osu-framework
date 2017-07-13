// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransformable
    {
        void WithDelay(double delay, Action action, bool recursive = false);

        double TransformStartTime { get; }

        void AddTransform(ITransform transform);

        void Flush(bool propagateChildren = false, Type flushType = null);
    }
}
