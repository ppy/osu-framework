// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics
{
    [Flags]
    public enum Invalidation
    {
        None = 0,

        DrawNode = 1 << 0,
        Presence = 1 << 1,
        ScreenSpaceDrawQuad = 1 << 2,
        DrawColourInfo = 1 << 3,
        DrawInfo = 1 << 4,
        RequiredParentSizeToFit = 1 << 5,
        BoundingBoxSizeBeforeParentAutoSize = 1 << 6,
        DrawSize = 1 << 7,
        BypassAutoSizeAxes = 1 << 8,

        // CompositeDrawable
        ChildSize = 1 << 10,
        ChildSizeBeforeAutoSize = 1 << 11,

        All = ~0,
    }
}
