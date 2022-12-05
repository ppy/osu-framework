// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Xunit;
using VerifyIncremental = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpMultiPhaseSourceGeneratorVerifier<osu.Framework.SourceGeneration.DependencyInjectionSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests
{
    public class DependencyInjectionMultiPhaseSourceGeneratorTests : AbstractGeneratorTests
    {
        // this function has support for the same test cases as DependencyInjectionSourceGeneratorTests.Check
        // but since the new verifier used here doesn't support creating nice diffs yet,
        // DependencyInjectionSourceGeneratorTests.Check is still preferred for now for single phase tests.
        [Theory]
        // Multi-phase tests:
        [InlineData("MultiPartialResolvedMember")]
        public void Check(string name)
        {
            GetTestSources(name,
                out (string filename, string content)[] commonSources,
                out (string filename, string content)[] sources,
                out (string filename, string content)[] commonGenerated,
                out (string filename, string content)[] generated
            );

            VerifyIncremental.Verify(commonSources, sources, commonGenerated, generated);
        }

        public static TheoryData<string, (int syntaxTargetCreated, int semanticTargetCreated, int emitHits)[]> CheckWithStatisticsData =>
            new TheoryData<string, (int syntaxTargetCreated, int semanticTargetCreated, int emitHits)[]>
            {
                {
                    "GeneratorCached", new[]
                    {
                        (2, 2, 2),
                        (0, 0, 0),
                    }
                },
                // TODO: fix this failing case
                // { "MultiPhasePartialCachedInterface", new[] {
                //     (3, 3, 3),
                //     (3, 2, 2), // should be (3, 2, 2) but current implementation returns (3, 1, 1), and generates invalid code due to a caching issue with single partials
                // } },
            };

        [Theory]
        [MemberData(nameof(CheckWithStatisticsData))]
        public void CheckWithStatistics(string name, (int, int, int)[] expectedStatistics)
        {
            GetTestSources(name,
                out (string filename, string content)[] commonSources,
                out (string filename, string content)[] sources,
                out (string filename, string content)[] commonGenerated,
                out (string filename, string content)[] generated
            );

            VerifyIncremental.Verify(commonSources, sources, commonGenerated, generated,
                test => test.AddStatisticsVerification(expectedStatistics));
        }
    }
}
