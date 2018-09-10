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
        DrawSize = 1 << 6,

        // CompositeDrawable
        AutoSize = 1 << 7,

        // FlowContainer
        ChildrenLayout = 1 << 8,

        // legacy compatibility
        LegacyRequiredParentSizeToFit = RequiredParentSizeToFit | DrawSize,
        LegacyMiscGeometry = RequiredParentSizeToFit | DrawInfo,

        All = ~0,
    }
}
