// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Represents a set of invalidation (possible change of properties) of <see cref="CompositeDrawable"/>.
    /// </summary>
    [Flags]
    public enum Invalidation
    {
        None = 0,

        DrawNode = 1 << 0,
        IsPresent = 1 << 1,
        ScreenSpaceDrawQuad = 1 << 2,
        DrawColourInfo = 1 << 3,
        DrawInfo = 1 << 4,
        RequiredParentSizeToFit = 1 << 5,
        BoundingBoxSizeBeforeParentAutoSize = 1 << 6,
        DrawSize = 1 << 7,
        BypassAutoSizeAxes = 1 << 8,
        Anchor = 1 << 9,
        Origin = 1 << 10,
        Size = 1 << 11,

        // CompositeDrawable
        ChildSize = 1 << 12,
        ChildSizeBeforeAutoSize = 1 << 13,
        RelativeChildSizeAndOffset = 1 << 14,   // RelativeChildSize or RelativeChildOffset

        /// <summary>
        /// All properties which invalidation can be propagated from parent to child.
        /// Consequently, a dependency of child to parent property must be one of them.
        /// </summary>
        MaskPropagateFromParent = DrawColourInfo | DrawInfo | DrawSize | ChildSize | ChildSizeBeforeAutoSize | RelativeChildSizeAndOffset,

        /// <summary>
        /// All properties which invalidation can be propagated from child to parent.
        /// Consequently, a dependency of parent to child property must be one of them.
        /// </summary>
        MaskPropagateFromChild = IsPresent | RequiredParentSizeToFit | BoundingBoxSizeBeforeParentAutoSize | BypassAutoSizeAxes | Anchor | Origin,

        All = ~0,
    }
}
