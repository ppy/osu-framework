// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = osu.Framework.SourceGeneration.Tests.Verifiers.CSharpSourceGeneratorVerifier<osu.Framework.SourceGeneration.Generators.UnbindAllBindables.UnbindAllBindablesSourceGenerator>;

namespace osu.Framework.SourceGeneration.Tests.UnbindAllBindables
{
    public class UnbindAllBindablesSourceGeneratorTests : AbstractGeneratorTests
    {
        protected override string ResourceNamespace => "UnbindAllBindables";

        [Theory]
        [InlineData("Basic")]
        [InlineData("AutoProperty")]
        [InlineData("ExplicitImplementation")]
        [InlineData("Static")]
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
