// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
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
            List<string> filenames = null!;

            AddStep("create resource store", () => resourceStore = new TestResourceStore());
            AddStep("store lookups for sample file", () => filenames = resourceStore.GetFilenames("test").ToList());

            AddStep("create sample store", () => audioManager.GetSampleStore(resourceStore));
            AddAssert("resource store lookups unchanged", () => resourceStore.GetFilenames("test"), () => Is.EquivalentTo(filenames));
        }

        private class TestResourceStore : ResourceStore<byte[]>
        {
            public new IEnumerable<string> GetFilenames(string lookup) => base.GetFilenames(lookup);
        }
    }
}
