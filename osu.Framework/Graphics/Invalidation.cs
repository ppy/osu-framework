// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Specifies which type of properties are being invalidated.
    /// </summary>
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
        BoundingBoxBeforeParentAutoSize = 1 << 6,
        DrawSize = 1 << 7,

        // CompositeDrawable
        AutoSize = 1 << 9,
        ChildSizeBeforeAutoSize = 1 << 10,
        ChildSize = 1 << 11,
        AliveInternalChildren = 1 << 12,

        // FlowContainer
        ChildrenLayout = 1 << 13,


        All = ~0,
    }
}
