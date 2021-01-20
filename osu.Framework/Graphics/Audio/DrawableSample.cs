// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Sample;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A <see cref="SampleChannel"/> wrapper to allow insertion in the draw hierarchy to allow transforms, lifetime management etc.
    /// </summary>
    public class DrawableSample : DrawableAudioWrapper, ISample
    {
        private readonly Sample sample;

        /// <summary>
        /// Construct a new drawable sample instance.
        /// </summary>
        /// <param name="sample">The audio sample to wrap.</param>
        /// <param name="disposeSampleOnDisposal">Whether the sample should be automatically disposed on drawable disposal/expiry.</param>
        public DrawableSample(Sample sample, bool disposeSampleOnDisposal = true)
            : base(sample, disposeSampleOnDisposal)
        {
            this.sample = sample;

            sample.Volume.Value = 0.5;
        }

        public SampleChannel Play() => sample.Play();

        public double Length => sample.Length;

        public int PlaybackConcurrency
        {
            get => sample.PlaybackConcurrency;
            set => sample.PlaybackConcurrency = value;
        }
    }
}
