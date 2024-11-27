// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using osu.Framework.SourceGeneration.Generators;

namespace osu.Framework.SourceGeneration.Tests.Verifiers
{
    public partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : AbstractIncrementalGenerator, new()
    {
        public class Test : CSharpSourceGeneratorTest<EmptySourceGeneratorProvider, DefaultVerifier>
        {
            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

            protected override IEnumerable<Type> GetSourceGenerators() => [typeof(TSourceGenerator)];

            protected override CompilationOptions CreateCompilationOptions()
            {
                return base.CreateCompilationOptions().WithOptimizationLevel(OptimizationLevel.Release);
            }

            protected override ParseOptions CreateParseOptions()
            {
                return ((CSharpParseOptions)base.CreateParseOptions()).WithLanguageVersion(LanguageVersion);
            }
        }
    }
}
