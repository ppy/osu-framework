// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Track
{
    public struct TrackAmplitudes
    {
        public float LeftChannel;
        public float RightChannel;

        public float Maximum => Math.Max(LeftChannel, RightChannel);

        public float Average => (LeftChannel + RightChannel) / 2;

        /// <summary>
        /// 256 length array of bins containing the average frequency of both channels at every ~78Hz step of the audible spectrum (0Hz - 20,000Hz).
        /// </summary>
        public float[] FrequencyAmplitudes;
    }
}
