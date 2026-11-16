// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Wasapi;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class WasapiExclusiveBufferLadderTest
    {
        [Test]
        public void TransientErrorsDoNotAdvanceBuffer()
        {
            Assert.That(WasapiExclusiveBufferLadder.IsTransientInitError(Errors.Busy), Is.True);
            Assert.That(WasapiExclusiveBufferLadder.IsTransientInitError(Errors.Driver), Is.True);
            Assert.That(WasapiExclusiveBufferLadder.IsTransientInitError(Errors.Unknown), Is.True);
        }

        [Test]
        public void NonTransientErrorsAdvanceBuffer()
        {
            Assert.That(WasapiExclusiveBufferLadder.IsTransientInitError(Errors.OK), Is.False);
            Assert.That(WasapiExclusiveBufferLadder.IsTransientInitError(Errors.Already), Is.False);
            Assert.That(WasapiExclusiveBufferLadder.IsTransientInitError(Errors.Init), Is.False);
        }
    }
}
