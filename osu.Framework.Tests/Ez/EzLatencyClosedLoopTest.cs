// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio.EzLatency;

namespace osu.Framework.Tests.Ez
{
    [TestFixture]
    public class EzLatencyClosedLoopTest
    {
        private EzLatencyAnalyzer analyzer;
        private readonly List<EzLatencyRecord> emitted = new List<EzLatencyRecord>();

        [SetUp]
        public void SetUp()
        {
            analyzer = new EzLatencyAnalyzer { Enabled = true };
            emitted.Clear();
            analyzer.OnNewRecord += r => emitted.Add(r);
        }

        [Test]
        public void AwaitAcoustic_PlayDoesNotEmitUntilHardware()
        {
            analyzer.AwaitAcoustic = true;

            double t0 = analyzer.GetCurrentTimestamp();
            analyzer.RecordInputData(t0, 1);
            analyzer.RecordPlaybackData(t0 + 2);

            Assert.That(emitted, Is.Empty);

            analyzer.RecordHardwareData(t0 + 40, 40, 0, 40);

            Assert.That(emitted, Has.Count.EqualTo(1));
            Assert.That(emitted[0].MeasuredMs, Is.EqualTo(2).Within(0.01));
            Assert.That(emitted[0].LatencyDifference, Is.EqualTo(40).Within(0.01));
            Assert.That(emitted[0].Note, Is.EqualTo(EzLatencyAnalyzer.NOTE_DIGITAL_OUTPUT_PATH));
        }

        [Test]
        public void WithoutAwaitAcoustic_PlayEmitsSoftwareOnly()
        {
            analyzer.AwaitAcoustic = false;

            double t0 = analyzer.GetCurrentTimestamp();
            analyzer.RecordInputData(t0, 1);
            analyzer.RecordPlaybackData(t0 + 3);

            Assert.That(emitted, Has.Count.EqualTo(1));
            Assert.That(emitted[0].MeasuredMs, Is.EqualTo(3).Within(0.01));
            Assert.That(emitted[0].Note, Is.EqualTo(EzLatencyAnalyzer.NOTE_BEST_EFFORT_NO_HW));
        }

        [Test]
        public void AwaitAcoustic_HardwareBeforePlay_WaitsThenEmitsTogether()
        {
            analyzer.AwaitAcoustic = true;

            double t0 = analyzer.GetCurrentTimestamp();
            analyzer.RecordInputData(t0, 1);
            analyzer.RecordHardwareData(t0 + 35, 35, 0, 35);

            Assert.That(emitted, Is.Empty);

            analyzer.RecordPlaybackData(t0 + 1.5);

            Assert.That(emitted, Has.Count.EqualTo(1));
            Assert.That(emitted[0].MeasuredMs, Is.EqualTo(1.5).Within(0.01));
            Assert.That(emitted[0].LatencyDifference, Is.EqualTo(35).Within(0.01));
            Assert.That(emitted[0].Note, Is.EqualTo(EzLatencyAnalyzer.NOTE_DIGITAL_OUTPUT_PATH));
        }

        [Test]
        public void AwaitAcoustic_TimeoutSoftEmitsSoftwareOnly()
        {
            analyzer.AwaitAcoustic = true;
            analyzer.TimeoutMs = 40;

            double t0 = analyzer.GetCurrentTimestamp();
            analyzer.RecordInputData(t0, 1);
            analyzer.RecordPlaybackData(t0 + 2);

            Assert.That(emitted, Is.Empty);

            Thread.Sleep(60);
            analyzer.PollTimeout();

            Assert.That(emitted, Has.Count.EqualTo(1));
            Assert.That(emitted[0].MeasuredMs, Is.EqualTo(2).Within(0.01));
            Assert.That(emitted[0].Note, Is.EqualTo(EzLatencyAnalyzer.NOTE_BEST_EFFORT_NO_HW));
            Assert.That(emitted[0].HardwareData.IsValid, Is.False);
        }

        [Test]
        public void Statistics_KeepsAcousticRoundtripSeparateFromSoftwarePlayback()
        {
            var collector = new EzLatencyCollector();

            collector.AddRecord(new EzLatencyRecord
            {
                InputTime = 100,
                PlaybackTime = 103,
                JudgeTime = 104,
                MeasuredMs = 3,
                Note = EzLatencyAnalyzer.NOTE_DIGITAL_OUTPUT_PATH,
                LatencyDifference = 42,
                OutputHardwareTime = 42,
                DriverTime = 142,
                InputData = new EzLatencyInputData { InputTime = 100, PlaybackTime = 103, JudgeTime = 104 },
                HardwareData = new EzLatencyHardwareData { DriverTime = 142, OutputHardwareTime = 42, LatencyDifference = 42 }
            });

            collector.AddRecord(new EzLatencyRecord
            {
                InputTime = 200,
                PlaybackTime = 201,
                MeasuredMs = 1,
                Note = EzLatencyAnalyzer.NOTE_BEST_EFFORT_NO_HW,
                InputData = new EzLatencyInputData { InputTime = 200, PlaybackTime = 201 },
                HardwareData = default
            });

            var stats = collector.GetStatistics();

            Assert.That(stats.RecordCount, Is.EqualTo(2));
            Assert.That(stats.AvgInputToPlayback, Is.EqualTo(2).Within(0.01)); // (3+1)/2
            Assert.That(stats.AcousticRecordCount, Is.EqualTo(1));
            Assert.That(stats.AvgAcousticRoundtrip, Is.EqualTo(42).Within(0.01));
            Assert.That(stats.MinAcousticRoundtrip, Is.EqualTo(42).Within(0.01));
            Assert.That(stats.MaxAcousticRoundtrip, Is.EqualTo(42).Within(0.01));
            Assert.That(stats.AvgHardwareLatency, Is.EqualTo(0)); // acoustic excluded from legacy hw avg
        }

        [Test]
        public void ComputeRms_SilenceIsZero()
        {
            byte[] buffer = new byte[4 * 64];

            float rms = AcousticLevelMath.ComputeRms(buffer, buffer.Length, AcousticLevelMath.SampleFormat.IeeeFloat32);

            Assert.That(rms, Is.EqualTo(0).Within(1e-6));
        }

        [Test]
        public void ComputeRms_ConstantFloatMatchesAmplitude()
        {
            const float amplitude = 0.25f;
            byte[] buffer = new byte[4 * 32];

            for (int i = 0; i < 32; i++)
                BitConverter.GetBytes(amplitude).CopyTo(buffer, i * 4);

            float rms = AcousticLevelMath.ComputeRms(buffer, buffer.Length, AcousticLevelMath.SampleFormat.IeeeFloat32);

            Assert.That(rms, Is.EqualTo(amplitude).Within(1e-5));
        }

        [Test]
        public void ComputeRms_Pcm16FullScaleNearOne()
        {
            byte[] buffer = new byte[2 * 16];

            for (int i = 0; i < 16; i++)
                BitConverter.GetBytes((short)32767).CopyTo(buffer, i * 2);

            float rms = AcousticLevelMath.ComputeRms(buffer, buffer.Length, AcousticLevelMath.SampleFormat.Pcm16);

            Assert.That(rms, Is.EqualTo(32767f / 32768f).Within(1e-4));
        }

        [Test]
        public void ComputeRms_Pcm24FullScaleNearOne()
        {
            byte[] buffer = new byte[3 * 8];

            for (int i = 0; i < 8; i++)
            {
                buffer[i * 3] = 0xFF;
                buffer[i * 3 + 1] = 0xFF;
                buffer[i * 3 + 2] = 0x7F;
            }

            float rms = AcousticLevelMath.ComputeRms(buffer, buffer.Length, AcousticLevelMath.SampleFormat.Pcm24);

            Assert.That(rms, Is.EqualTo(8388607f / 8388608f).Within(1e-4));
        }

        [Test]
        public void ComputeRms_Pcm32FullScaleNearOne()
        {
            byte[] buffer = new byte[4 * 8];

            for (int i = 0; i < 8; i++)
                BitConverter.GetBytes(int.MaxValue).CopyTo(buffer, i * 4);

            float rms = AcousticLevelMath.ComputeRms(buffer, buffer.Length, AcousticLevelMath.SampleFormat.Pcm32);

            Assert.That(rms, Is.EqualTo(int.MaxValue / 2147483648f).Within(1e-4));
        }

        [Test]
        public void OutputPathProbe_ObserveFloat_RecordsWhenAboveThreshold()
        {
            var hardware = new List<(double driver, double outHw, double inHw, double lat)>();
            var probe = new OutputPathAcousticProbe(
                () => 100,
                () => { },
                (d, o, i, l) => hardware.Add((d, o, i, l)))
            {
                Threshold = 0.02f
            };

            probe.Arm(50);

            const float amplitude = 0.25f;
            byte[] buffer = new byte[4 * 64];

            for (int i = 0; i < 64; i++)
                BitConverter.GetBytes(amplitude).CopyTo(buffer, i * 4);

            probe.Observe(buffer, buffer.Length, AcousticLevelMath.SampleFormat.IeeeFloat32);

            Assert.That(hardware, Has.Count.EqualTo(1));
            Assert.That(hardware[0].lat, Is.EqualTo(50).Within(0.01));
            Assert.That(probe.IsArmed, Is.False);
        }

        [Test]
        public void OutputPathProbe_IgnoreWindow_SkipsEarlyEnergy()
        {
            double now = 0;
            var hardware = new List<double>();
            var probe = new OutputPathAcousticProbe(
                () => now,
                () => { },
                (_, o, _, _) => hardware.Add(o))
            {
                Threshold = 0.02f
            };

            probe.Arm(0);
            now = 4; // within ignore_after_arm_ms = 8

            byte[] buffer = new byte[4 * 32];

            for (int i = 0; i < 32; i++)
                BitConverter.GetBytes(0.5f).CopyTo(buffer, i * 4);

            probe.Observe(buffer, buffer.Length, AcousticLevelMath.SampleFormat.IeeeFloat32);

            Assert.That(hardware, Is.Empty);
            Assert.That(probe.IsArmed, Is.True);
        }
    }
}
