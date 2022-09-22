// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;
using osu.Framework.Tests.Visual;

namespace osu.Framework.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkWaveform : BenchmarkTest
    {
        private Stream data = null!;
        private Waveform preloadedWaveform = null!;

        public override void SetUp()
        {
            base.SetUp();

            Bass.Init();

            var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(FrameworkTestScene).Assembly), "Resources");

            data = store.GetStream("Tracks/sample-track.mp3");

            Debug.Assert(data != null);

            preloadedWaveform = new Waveform(data);
            // wait for load
            preloadedWaveform.GetPoints();
        }

        [Benchmark]
        [Test]
        public async Task TestCreate()
        {
            var waveform = new Waveform(data);

            var originalPoints = await waveform.GetPointsAsync().ConfigureAwait(false);
            Debug.Assert(originalPoints.Length > 0);
        }

        [Arguments(1024)]
        [TestCase(1024)]
        [Arguments(32768)]
        [TestCase(32768)]
        [Benchmark]
        public async Task TestResample(int size)
        {
            var resampled = await preloadedWaveform.GenerateResampledAsync(size).ConfigureAwait(false);
            var resampledPoints = await resampled.GetPointsAsync().ConfigureAwait(false);

            Debug.Assert(resampledPoints.Length > 0);
        }
    }
}
