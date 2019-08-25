// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneLoopingSample : FrameworkTestScene
    {
        private SampleChannel sampleChannel;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleChannel = audio.GetSampleStore(new NamespacedResourceStore<byte[]>(new DllResourceStore("osu.Framework.Tests.dll"), "Resources")).Get("Samples.tone.wav");

            // reduce volume of the tone due to how loud it normally is.
            if (sampleChannel != null)
                sampleChannel.Volume.Value = 0.05;
        }

        [Test]
        public void TestLooping()
        {
            AddAssert("not looping", () => !sampleChannel.Looping);

            AddStep("enable looping", () => sampleChannel.Looping = true);
            AddStep("play sample", () => sampleChannel.Play());
            AddAssert("is playing", () => sampleChannel.Playing);

            AddWaitStep("wait", 1);
            AddAssert("is still playing", () => sampleChannel.Playing);

            AddStep("stop sample", () => sampleChannel.Stop());
            AddAssert("not playing", () => !sampleChannel.Playing);
        }
    }
}
