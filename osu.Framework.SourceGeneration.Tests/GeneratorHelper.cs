// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace osu.Framework.SourceGeneration.Tests
{
    public static class GeneratorTestHelper
    {
        public static void VerifyZeroDiagnostics(this GeneratorDriverRunResult runResult)
        {
            var compilationDiagnostics = runResult.Diagnostics
                                                  .Where(d => d.Severity == DiagnosticSeverity.Error)
                                                  .ToArray();
            var generatorDiagnostics = runResult.Results
                                                .SelectMany(r => r.Diagnostics)
                                                .Where(d => d.Severity == DiagnosticSeverity.Error)
                                                .ToArray();

            if (compilationDiagnostics.Any() || generatorDiagnostics.Any())
            {
                var sb = new StringBuilder();

                sb.AppendLine("Expected no diagnostics, but found:");

                foreach (var d in compilationDiagnostics)
                    sb.AppendLine($"Compilation: {d}");

                foreach (var d in generatorDiagnostics)
                    sb.AppendLine($"Generator: {d}");

                throw new Xunit.Sdk.XunitException(sb.ToString());
            }
        }

        public static void VerifyMultiPhaseGeneratedSources(this GeneratorDriverRunResult runResult, (string filename, string content)[] files, int phase)
        {
            var generatedSources = runResult.Results
                                            .SelectMany(r => r.GeneratedSources)
                                            .ToDictionary(s => s.HintName);

            if (generatedSources.Count != files.Length)
                throw new Xunit.Sdk.XunitException($"Phase {phase}: Expected {files.Length} generated sources, but found {generatedSources.Count}");

            int matches = 0;

            foreach (var (filename, content) in files)
            {
                if (!generatedSources.TryGetValue(filename, out var source))
                    throw new Xunit.Sdk.XunitException($"Phase {phase}: Expected generated source {filename}, but it was not found");

                string actual = source.SourceText.ToString();

                new XUnitVerifier().EqualOrDiff(content, actual, $"Phase {phase}: Generated source {filename} did not match expected content");

                matches++;
            }

            if (matches != files.Length)
                throw new Xunit.Sdk.XunitException($"Phase {phase}: Expected {files.Length} generated sources, but found {matches}");
        }
    }

    public class IncrementalCompilation
    {
        private readonly Dictionary<string, SyntaxTree> sources = new Dictionary<string, SyntaxTree>();

        public Compilation Compilation { get; private set; }

        public IncrementalCompilation()
        {
            Compilation = CSharpCompilation.Create("test",
                references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        public GeneratorDriverRunResult RunGenerators(ref GeneratorDriver driver)
        {
            driver = driver.RunGeneratorsAndUpdateCompilation(Compilation, out _, out _);
            return driver.GetRunResult();
        }

        public void AddOrUpdateSource(string filename, string content)
        {
            var newTree = CSharpSyntaxTree.ParseText(content, path: filename);

            if (sources.ContainsKey(filename))
            {
                var oldTree = sources[filename];
                sources[filename] = newTree;

                if (newTree.ToString() == oldTree.ToString())
                {
                    return;
                }

                Compilation = Compilation.ReplaceSyntaxTree(oldTree, newTree);
            }
            else
            {
                sources.Add(filename, newTree);
                Compilation = Compilation.AddSyntaxTrees(newTree);
            }
        }

        public void RemoveSource(string filename)
        {
            var oldTree = sources[filename];
            sources.Remove(filename);
            Compilation = Compilation.RemoveSyntaxTrees(oldTree);
        }
    }
}
