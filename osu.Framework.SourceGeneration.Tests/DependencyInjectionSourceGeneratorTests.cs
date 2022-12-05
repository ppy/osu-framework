// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpSourceGeneratorVerifier<osu.Framework.SourceGeneration.DependencyInjectionSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests
{
    public class DependencyInjectionSourceGeneratorTests : AbstractGeneratorTests
    {
        [Theory]
        // Non-partial-class tests:
        [InlineData("EmptyFile")]
        [InlineData("EmptyClass")]
        [InlineData("EmptyDrawable")]
        [InlineData("CachedClass")]
        [InlineData("CachedDrawable")]
        // Partial-class tests:
        [InlineData("DependencyInjectionCandidateWithConflictingLocal")]
        [InlineData("DependencyInjectionCandidateWithConflictingNestedClass")]
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
        public Task Check(string name)
        {
            GetTestSources(name,
                out (string filename, string content)[] commonSourceFiles,
                out (string filename, string content)[] sourceFiles,
                out (string filename, string content)[] commonGeneratedFiles,
                out (string filename, string content)[] generatedFiles
            );

            return VerifyCS.VerifyAsync(commonSourceFiles, sourceFiles, commonGeneratedFiles, generatedFiles);
        }
    }
}
