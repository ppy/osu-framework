// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Audio
{
    [HeadlessTest]
    public partial class TestSceneAudioManager : FrameworkTestScene
    {
        [Resolved]
        private AudioManager audioManager { get; set; } = null!;

        [Test]
        public void TestSampleStoreCreationDoesNotMutateOriginalResourceStore()
        {
            TestResourceStore resourceStore = null!;
            ISampleStore sampleStore = null!;
            List<string> filenames = null!;

            AddStep("create resource store", () => resourceStore = new TestResourceStore());
            AddStep("store lookups for sample file", () => filenames = resourceStore.GetFilenames("test").ToList());

            AddStep("create sample store", () => sampleStore = audioManager.GetSampleStore(resourceStore));
            AddAssert("resource store lookups unchanged", () => resourceStore.GetFilenames("test"), () => Is.EquivalentTo(filenames));

            AddStep("attempt to look up sample", () => sampleStore.Get("sample"));
            AddAssert("extension lookups attempted", () => resourceStore.AttemptedLookups, () => Is.EquivalentTo(new[] { "sample", "sample.wav", "sample.mp3" }));
        }

        [Test]
        public void TestSampleStoreWithAdditionalExtensions()
        {
            TestResourceStore resourceStore = null!;
            ISampleStore sampleStore = null!;

            AddStep("create resource store", () => resourceStore = new TestResourceStore());
            AddStep("create sample store", () => sampleStore = audioManager.GetSampleStore(resourceStore));
            AddStep("add another extension", () => sampleStore.AddExtension("ogg"));

            AddStep("attempt to look up sample", () => sampleStore.Get("sample"));
            AddAssert("extension lookups attempted", () => resourceStore.AttemptedLookups,
                () => Is.EquivalentTo(new[] { "sample", "sample.wav", "sample.mp3", "sample.ogg" }));
        }

        private class TestResourceStore : ResourceStore<byte[]>
        {
            public new IEnumerable<string> GetFilenames(string lookup) => base.GetFilenames(lookup);

            private readonly List<string> attemptedLookups = new List<string>();
            public IEnumerable<string> AttemptedLookups => attemptedLookups;

            public override byte[] Get(string name)
            {
                attemptedLookups.Add(name);
                return base.Get(name);
            }

            public override Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default)
            {
                attemptedLookups.Add(name);
                return base.GetAsync(name, cancellationToken);
            }
        }
    }
}
