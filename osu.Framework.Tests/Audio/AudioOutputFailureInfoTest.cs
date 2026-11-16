// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.EzLatency;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AudioOutputFailureInfoTest
    {
        [Test]
        public void CreateIncludesModeDeviceAndError()
        {
            var info = AudioManager.AudioOutputFailureInfo.Create(
                AudioOutputMode.WasapiExclusive,
                "Speakers (WASAPI Exclusive)",
                "Busy");

            Assert.That(info.Mode, Is.EqualTo(AudioOutputMode.WasapiExclusive));
            Assert.That(info.BassError, Is.EqualTo("Busy"));
            Assert.That(info.Message, Does.Contain("WASAPI Exclusive"));
            Assert.That(info.Message, Does.Contain("Speakers (WASAPI Exclusive)"));
            Assert.That(info.Message, Does.Contain("Busy"));
        }

        [Test]
        public void CreateFallsBackWhenErrorMissing()
        {
            var info = AudioManager.AudioOutputFailureInfo.Create(AudioOutputMode.Asio, string.Empty, null);

            Assert.That(info.BassError, Is.Null);
            Assert.That(info.Message, Does.Contain("ASIO"));
            Assert.That(info.Message, Does.Contain("default device"));
            Assert.That(info.Message, Does.Contain("unknown"));
        }
    }
}
