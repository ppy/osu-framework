// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Framework.Configuration
{
    public enum FrameSync
    {
        VSync,

        [Description("2x refresh rate")]
        Limit2x,

        [Description("4x refresh rate")]
        Limit4x,

        [Description("8x refresh rate")]
        Limit8x,

        [Description("Basically unlimited")]
        Unlimited,
    }
}
