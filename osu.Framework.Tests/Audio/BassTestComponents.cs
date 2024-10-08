// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using ManagedBass;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    /// <summary>
    /// Provides a BASS audio pipeline to be used for testing audio components.
    /// </summary>
    public class BassTestComponents : AudioTestComponents
    {
        public BassTestComponents(bool init = true)
            : base(init)
        {
        }

        public override void Init()
        {
            AudioThread.PreloadBass();

            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Configure(ManagedBass.Configuration.UpdatePeriod, 5);
            Bass.Init(0);
        }

        public override AudioMixer CreateMixer()
        {
            var mixer = new BassAudioMixer(null, Mixer, "Test mixer");
            MixerComponents.AddItem(mixer);
            return mixer;
        }

        public override void DisposeInternal()
        {
            base.DisposeInternal();
            Bass.Free();
        }

        internal override Track CreateTrack(Stream data, string name) => new TrackBass(data, name);

        internal override SampleFactory CreateSampleFactory(Stream stream, string name, AudioMixer mixer, int playbackConcurrency)
        {
            byte[] data;

            using (stream)
                data = stream.ReadAllBytesToArray();

            return new SampleBassFactory(data, name, (BassAudioMixer)mixer, playbackConcurrency);
        }
    }
}
