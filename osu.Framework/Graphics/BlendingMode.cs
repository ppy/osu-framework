// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Graphics
{
    [Obsolete("Use BlendingParameters statics instead")] // can be removed 20200220
    public static class BlendingMode
    {
        /// <summary>
        /// Inherits from parent.
        /// </summary>
        [Obsolete("Use BlendingParameters statics instead")] // can be removed 20200220
        public static BlendingParameters Inherit => BlendingParameters.Inherit;

        /// <summary>
        /// Mixes with existing colour by a factor of the colour's alpha.
        /// </summary>
        [Obsolete("Use BlendingParameters statics instead")] // can be removed 20200220
        public static BlendingParameters Mixture => BlendingParameters.Mixture;

        /// <summary>
        /// Purely additive (by a factor of the colour's alpha) blending.
        /// </summary>
        [Obsolete("Use BlendingParameters statics instead")] // can be removed 20200220
        public static BlendingParameters Additive => BlendingParameters.Additive;

        /// <summary>
        /// No alpha blending whatsoever.
        /// </summary>
        [Obsolete("Use BlendingParameters statics instead")] // can be removed 20200220
        public static BlendingParameters None => BlendingParameters.None;
    }
}
