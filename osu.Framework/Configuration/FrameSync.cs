// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
        [Description("Unlimited")]
        Unlimited,
    }
}
