// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpMultiPhaseSourceGeneratorVerifier<osu.Framework.SourceGeneration.DependencyInjectionSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests
{
    public class DependencyInjectionMultiPhaseSourceGeneratorTests : AbstractGeneratorTests
    {
        [Theory]
        // Non-partial-class tests:
        [InlineData("EmptyFile")]
        [InlineData("EmptyClass")]
        [InlineData("EmptyDrawable")]
        [InlineData("CachedClass")]
        [InlineData("CachedDrawable")]
        // Partial-class tests:
        [InlineData("EmptyPartialDrawable")]
        [InlineData("GenericEmptyPartialDrawable")]
        [InlineData("PartialCachedClass")]
        [InlineData("PartialCachedDrawable")]
        [InlineData("PartialCachedMember")]
        [InlineData("PartialCachedInterface")]
        [InlineData("PartialResolvedMember")]
        [InlineData("PartialResolvedBindable")]
        [InlineData("PartialResolvedNullableBindable")]
        [InlineData("PartialBackgroundDependencyLoadedDrawable")]
        [InlineData("PartialNestedClasses")]
        [InlineData("NestedCachedClass")]
        [InlineData("MultipleCachedMember")]
        [InlineData("CachedInheritedInterface")]
        [InlineData("CachedBaseType")]
        // Multi-phase tests:
        [InlineData("MultiPartialResolvedMember")]
        public async Task Check(string name) => await RunTest(name).ConfigureAwait(false);

        protected override Task Verify(
            (string filename, string content)[] commonSources,
            (string filename, string content)[] sources,
            (string filename, string content)[] commonGenerated,
            (string filename, string content)[] generated)
        => Task.Run(() => VerifyCS.Verify(commonSources, sources, commonGenerated, generated));
    }
}
