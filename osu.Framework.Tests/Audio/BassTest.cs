// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using NUnit.Framework;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Audio
{
    public abstract class BassTest : AudioTest
    {
        public override void Setup()
        {
            base.Setup();

            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Init(0);
        }
    }
}
