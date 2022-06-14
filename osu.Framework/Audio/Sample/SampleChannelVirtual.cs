// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// A <see cref="SampleChannel"/> which explicitly plays no audio.
    /// Aimed for scenarios in which a non-null <see cref="SampleChannel"/> is needed, but one that doesn't necessarily play any sound.
    /// </summary>
    internal class SampleChannelVirtual : SampleChannel
    {
        private volatile bool playing = true;

        public override bool Playing => playing;

        protected override void UpdateState()
        {
            base.UpdateState();

            if (!Looping)
                Stop();
        }

        public override void Stop()
        {
            base.Stop();
            playing = false;
        }
    }
}
