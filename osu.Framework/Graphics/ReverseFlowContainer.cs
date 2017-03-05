// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics
{
    public class ReverseFillFlowContainer : ReverseFillFlowContainer<Drawable>
    {
    }

    public class ReverseFillFlowContainer<T> : FillFlowContainer<T>
        where T : Drawable
    {
        protected override IComparer<Drawable> DepthComparer => new ReverseCreationOrderDepthComparer();
    }
}
