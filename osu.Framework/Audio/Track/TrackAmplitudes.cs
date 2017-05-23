// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Audio.Track
{
    public struct TrackAmplitudes
    {
        public float LeftChannel;
        public float RightChannel;

        public float Maximum => Math.Max(LeftChannel, RightChannel);

        public float Average => (LeftChannel + RightChannel) / 2;
    }
}