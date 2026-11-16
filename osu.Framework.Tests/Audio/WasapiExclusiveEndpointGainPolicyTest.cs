// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio.Windows;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class WasapiExclusiveEndpointGainPolicyTest
    {
        [Test]
        public void MuteForcesSilence()
            => Assert.That(WasapiExclusiveEndpointGainPolicy.MixerVolume(1f, true), Is.EqualTo(0f));

        [Test]
        public void ScalarIsClamped()
        {
            Assert.That(WasapiExclusiveEndpointGainPolicy.MixerVolume(-0.2f, false), Is.EqualTo(0f));
            Assert.That(WasapiExclusiveEndpointGainPolicy.MixerVolume(0.25f, false), Is.EqualTo(0.25f));
            Assert.That(WasapiExclusiveEndpointGainPolicy.MixerVolume(1.5f, false), Is.EqualTo(1f));
        }

        [Test]
        public void InvalidScalarLeavesUnityGain()
        {
            Assert.That(WasapiExclusiveEndpointGainPolicy.MixerVolume(float.NaN, false), Is.EqualTo(1f));
            Assert.That(WasapiExclusiveEndpointGainPolicy.MixerVolume(float.PositiveInfinity, false), Is.EqualTo(1f));
        }
    }
}
