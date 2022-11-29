// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace osu.Framework.SourceGeneration.Tests.Verifiers
{
    public partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, XUnitVerifier>
        {
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

            protected override IEnumerable<ISourceGenerator> GetSourceGenerators() => new[]
            {
                new TSourceGenerator().AsSourceGenerator()
            };

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }
        }
    }
}
