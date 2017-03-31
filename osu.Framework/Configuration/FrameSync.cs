// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    public enum FrameSync
    {
        VSync = 1,
        Limit120 = 0,
        Unlimited = 2,
        CompletelyUnlimited = 4,
        Custom = 5
    }
}
