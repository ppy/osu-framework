// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpSourceGeneratorVerifier<osu.Framework.SourceGeneration.Generators.LongRunningLoad.LongRunningLoadSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests.LongRunningLoad
{
    public class LongRunningLoadSourceGeneratorTests : AbstractGeneratorTests
    {
        protected override string ResourceNamespace => "LongRunningLoad";

        [Theory]
        [InlineData("LongRunningType")]
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
