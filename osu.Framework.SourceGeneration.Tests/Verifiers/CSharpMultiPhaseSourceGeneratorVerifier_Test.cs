// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace osu.Framework.SourceGeneration.Tests.Verifiers
{
    public partial class CSharpMultiPhaseSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        public class Test
        {
            private GeneratorDriver driver;
            private readonly (string filename, string content)[] commonSources;
            private readonly (string filename, string content)[] commonGenerated;
            private readonly List<List<(string filename, string content)>> multiPhaseSources;
            private readonly List<List<(string filename, string content)>> multiPhaseGenerated;

            public LanguageVersion LanguageVersion { get; set; } = LanguageVersion.Default;

            public event Action<int>? PhaseChanged;
            public event Action? PhaseCompleted;

            public Test(
                (string filename, string content)[] commonSources,
                (string filename, string content)[] commonGenerated,
                List<List<(string filename, string content)>> multiPhaseSources,
                List<List<(string filename, string content)>> multiPhaseGenerated)
            {
                driver = CSharpGeneratorDriver.Create(new TSourceGenerator());
                driver = driver.WithUpdatedParseOptions(CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion));
                this.commonSources = commonSources;
                this.commonGenerated = commonGenerated;
                this.multiPhaseSources = multiPhaseSources;
                this.multiPhaseGenerated = multiPhaseGenerated;
            }

            public void AddStatisticsVerification((int syntaxTargetCreated, int semanticTargetCreated, int emitHits)[] expectedStatistics)
            {
                int phase = 0;
                int syntaxTargetCreated = 0;
                int semanticTargetCreated = 0;
                int emitHits = 0;

                PhaseChanged += p =>
                {
                    phase = p;
                    syntaxTargetCreated = 0;
                    semanticTargetCreated = 0;
                    emitHits = 0;
                };

                DependencyInjectionSourceGenerator.GeneratorEvent.SyntaxTargetCreated += _ => syntaxTargetCreated++;
                DependencyInjectionSourceGenerator.GeneratorEvent.SemanticTargetCreated += _ => semanticTargetCreated++;
                DependencyInjectionSourceGenerator.GeneratorEvent.Emit += _ => emitHits++;

                PhaseCompleted += () =>
                {
                    var expected = expectedStatistics[phase];
                    var actual = (syntaxTargetCreated, semanticTargetCreated, emitHits);

                    if (actual != expected)
                        throw new Xunit.Sdk.XunitException($"Phase {phase}: Expected statistics {expected} but got {actual}.");
                };
            }

            public void Verify()
            {
                IncrementalCompilation compilation = new IncrementalCompilation();

                foreach (var s in commonSources)
                    compilation.AddOrUpdateSource(s.filename, s.content);

                for (int phase = 0; phase < multiPhaseSources.Count; phase++)
                {
                    PhaseChanged?.Invoke(phase);
                    List<(string filename, string content)> sources = multiPhaseSources[phase];
                    List<(string filename, string content)> generated = multiPhaseGenerated[phase];
                    generated.AddRange(commonGenerated);

                    // Remove sources from previous phase that are not existing in the current phase
                    if (phase > 0)
                    {
                        foreach (var (filename, _) in multiPhaseSources[phase - 1].Where(old => sources.All(@new => @new.filename != old.filename)))
                            compilation.RemoveSource(filename);
                    }

                    // Add sources for the current phase
                    foreach (var (filename, content) in sources)
                    {
                        compilation.AddOrUpdateSource(filename, content);
                    }

                    // Run the generator. This will update compilation internally and return the results of the
                    // run, which we use later to verify the generated sources. We pass the driver as ref since
                    // driver itself is immutable and creates a new driver instance per run with new information.
                    var results = compilation.RunGenerators(ref driver);

                    results.VerifyZeroDiagnostics();
                    results.VerifyMultiPhaseGeneratedSources(generated.ToArray(), phase);
                    PhaseCompleted?.Invoke();
                }
            }
        }
    }
}
