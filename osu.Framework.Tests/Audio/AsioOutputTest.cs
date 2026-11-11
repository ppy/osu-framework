// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;
using NUnit.Framework;
using osu.Framework.Audio.Asio;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AsioOutputTest
    {
        [TestCase(0, 32, 1024, -1, true)]
        [TestCase(64, 32, 1024, -1, true)]
        [TestCase(96, 32, 1024, -1, false)]
        [TestCase(16, 32, 1024, -1, false)]
        [TestCase(2048, 32, 1024, -1, false)]
        [TestCase(1024, 32, 1024, -1, true)]
        [TestCase(96, 32, 1024, 32, true)]
        [TestCase(100, 32, 1024, 32, false)]
        [TestCase(64, 64, 64, 0, true)]
        [TestCase(128, 64, 64, 0, false)]
        [TestCase(96, 32, 1024, 0, true)]
        public void TestDriverBufferConstraints(int size, int minimum, int maximum, int granularity, bool expected)
            => Assert.That(AsioOutput.IsBufferSizeSupported(size, minimum, maximum, granularity), Is.EqualTo(expected));

        [Test]
        public void TestMissingDriverReleasesResources()
        {
            if (!OperatingSystem.IsWindows())
                Assert.Ignore("ASIO requires Windows.");

            Assert.Throws<InvalidOperationException>(() =>
            {
                using var output = new AsioOutput();
                output.Initialise("osu! nonexistent ASIO test driver", 48000, 64);
            });
        }

        [Test]
        public void TestHardwareStereoPullAndReinitialisation()
        {
            string? driver = Environment.GetEnvironmentVariable("OSU_ASIO_TEST_DRIVER");
            int bufferSize = int.TryParse(Environment.GetEnvironmentVariable("OSU_ASIO_TEST_BUFFER"), out int requestedBuffer) ? requestedBuffer : 0;

            if (!OperatingSystem.IsWindows() || string.IsNullOrEmpty(driver))
                Assert.Ignore("Set OSU_ASIO_TEST_DRIVER to an installed driver to run the hardware test.");

            Assert.That(Bass.Init(Bass.NoSoundDevice), Is.True, $"BASS: {Bass.LastError}");

            try
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    using var rejectedOutput = new AsioOutput();
                    rejectedOutput.Initialise(driver, 48000, int.MaxValue);
                });

                // Reopening verifies shutdown releases both the ASIO device and callback threads.
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    var output = new AsioOutput();
                    int source = 0;
                    int submixer = 0;
                    StreamProcedure? silence = null;

                    try
                    {
                        output.Initialise(driver, 48000, bufferSize);
                        long[] bytesPulled = new long[1];
                        byte[] silentBuffer = new byte[65536];
                        silence = (_, buffer, length, _) =>
                        {
                            for (int offset = 0; offset < length; offset += silentBuffer.Length)
                                Marshal.Copy(silentBuffer, 0, IntPtr.Add(buffer, offset), Math.Min(silentBuffer.Length, length - offset));

                            Interlocked.Add(ref bytesPulled[0], length);
                            return length;
                        };

                        // Use the same nested decoding mixer layout as music and hitsounds.
                        source = Bass.CreateStream(44100, 2, BassFlags.Decode | BassFlags.Float, silence);
                        submixer = BassMix.CreateMixerStream(44100, 2, BassFlags.Decode | BassFlags.MixerNonStop | BassFlags.MixerPositionEx);
                        Assert.That(source, Is.Not.Zero);
                        Assert.That(submixer, Is.Not.Zero);

                        Assert.That(BassMix.MixerAddChannel(submixer, source, BassFlags.MixerChanBuffer), Is.True);
                        Assert.That(BassMix.MixerAddChannel(output.MixerHandle, submixer, BassFlags.MixerChanBuffer), Is.True);
                        output.Start(bufferSize);
                        TestContext.Out.WriteLine(output.Status);
                        Thread.Sleep(300);
                        Assert.That(Interlocked.Read(ref bytesPulled[0]), Is.GreaterThan(44100 * 2 * sizeof(float) / 10));
                        Assert.That(BassMix.ChannelGetPosition(source), Is.GreaterThan(0), "Playback clock should advance through nested decoding mixers.");
                        Assert.That(Bass.ChannelGetAttribute(output.MixerHandle, ChannelAttribute.MixerLatency, out float latency), Is.True);
                        Assert.That(latency, Is.GreaterThan(0).And.LessThan(0.1f));
                        output.Dispose();
                        long stoppedAt = Interlocked.Read(ref bytesPulled[0]);
                        Thread.Sleep(30);
                        Assert.That(Interlocked.Read(ref bytesPulled[0]), Is.EqualTo(stoppedAt), "Callbacks must stop before mixer disposal returns.");
                    }
                    finally
                    {
                        output.Dispose();

                        if (submixer != 0)
                            Bass.StreamFree(submixer);
                        if (source != 0)
                            Bass.StreamFree(source);

                        GC.KeepAlive(silence);
                    }
                }
            }
            finally
            {
                Bass.Free();
            }
        }
    }
}
