// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Audio.Sample
{
    /// <summary>
    /// A <see cref="Sample"/> which explicitly plays no audio.
    /// Aimed for scenarios in which a non-null <see cref="Sample"/> is needed, but one that doesn't necessarily play any sound.
    /// </summary>
    public sealed class SampleVirtual : Sample
    {
        protected override SampleChannel CreateChannel() => new SampleChannelVirtual();
    }
}
