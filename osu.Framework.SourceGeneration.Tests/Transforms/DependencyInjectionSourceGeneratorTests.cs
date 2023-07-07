// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpSourceGeneratorVerifier<osu.Framework.SourceGeneration.Generators.Transforms.TransformsSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests.Transforms
{
    public class TransformsSourceGeneratorTests : AbstractGeneratorTests
    {
        protected override string ResourceNamespace => "Transforms";

        [Theory]
        [InlineData("Test")]
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
