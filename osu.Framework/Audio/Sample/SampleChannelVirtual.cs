// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// A <see cref="SampleChannel"/> which explicitly plays no audio.
    /// Aimed for scenarios in which a non-null <see cref="SampleChannel"/> is needed, but one that doesn't necessarily play any sound.
    /// </summary>
    public sealed class SampleChannelVirtual : SampleChannel
    {
        public SampleChannelVirtual()
            : base(new SampleVirtual(), _ => { })
        {
        }

        public override bool Playing => false;

        private class SampleVirtual : Sample
        {
        }
    }
}
