// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace osu.Framework.SourceGeneration.Tests.Verifiers
{
    public partial class CSharpSourceGeneratorVerifier<TSourceGenerator>
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        public static async Task VerifyAsync(
            (string filename, string content)[] commonSources,
            (string filename, string content)[] sources,
            (string filename, string content)[] commonGenerated,
            (string filename, string content)[] generated)
        {
            var test = new Test();

            foreach (var s in commonSources)
                test.TestState.Sources.Add((s.filename, SourceText.From(s.content, Encoding.UTF8)));

            foreach (var s in sources)
                test.TestState.Sources.Add((s.filename, SourceText.From(s.content, Encoding.UTF8)));

            foreach (var e in commonGenerated)
                test.TestState.GeneratedSources.Add((typeof(TSourceGenerator), e.filename, SourceText.From(e.content, Encoding.UTF8)));

            foreach (var e in generated)
                test.TestState.GeneratedSources.Add((typeof(TSourceGenerator), e.filename, SourceText.From(e.content, Encoding.UTF8)));

            await test.RunAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
