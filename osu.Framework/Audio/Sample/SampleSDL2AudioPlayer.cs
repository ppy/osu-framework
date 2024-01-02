// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Sample
{
    internal class SampleSDL2AudioPlayer : ResamplingPlayer
    {
        private int position;

        private volatile bool done;
        public bool Done => done;

        private readonly float[] audioData;

        public bool Loop { get; set; }

        public SampleSDL2AudioPlayer(float[] audioData, int rate, byte channels)
            : base(rate, channels)
        {
            this.audioData = audioData;
        }

        protected override int GetRemainingRawFloats(float[] data, int offset, int needed)
        {
            if (audioData.Length <= 0)
            {
                done = true;
                return 0;
            }

            int i = 0;

            while (i < needed)
            {
                int put = Math.Min(needed - i, audioData.Length - position);

                if (put > 0)
                    Array.Copy(audioData, position, data, offset + i, put);

                i += put;
                position += put;

                // done playing
                if (position >= audioData.Length)
                {
                    if (Loop) // back to start if looping
                        position = 0;
                    else
                    {
                        done = true;
                        break;
                    }
                }
            }

            return i;
        }

        public void Reset(bool resetIndex = true)
        {
            done = false;
            if (resetIndex)
                position = 0;
        }
    }
}
