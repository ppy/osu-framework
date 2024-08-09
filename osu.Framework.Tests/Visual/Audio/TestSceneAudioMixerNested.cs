// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics.Visualisation.Audio;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneAudioMixerNested : FrameworkTestScene
    {
        private AudioMixer parentMixer;
        private AudioMixer subMixer;
        private Sample sample;

        private AudioMixerVisualiser visualiser;

        [Resolved]
        private AudioManager audio { get; set; }

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            cleanup();

            parentMixer = audio.CreateAudioMixer("test mixer parent");
            subMixer = audio.CreateAudioMixer(parentMixer, "test mixer child");

            sample = audio.Samples.Get("long.mp3");
            sample.Volume.Value = 0.5f;

            Child = visualiser = new AudioMixerVisualiser();
            visualiser.Show();

            var channel = sample.GetChannel();
            subMixer.Add(channel);
            channel.Looping = true;
            channel.Play();
        });

        [Test]
        public void TestNestedMixers()
        {
            AddStep("set Mixer to parentMixer", () => subMixer.Mixer = parentMixer);
            AddStep("set Mixer to GlobalMixer", () => subMixer.Mixer = audio.GlobalMixer);
            AddStep("set Mixer to null", () => subMixer.Mixer = null);
        }

        private void cleanup()
        {
            parentMixer?.Dispose();
            subMixer?.Dispose();
            sample?.Dispose();
        }

        protected override void Dispose(bool isDisposing)
        {
            cleanup();
            base.Dispose(isDisposing);
        }
    }
}
