// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    [Generator]
    public class DependencyInjectionSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<GeneratorClassCandidate> candidateClasses =
                context.SyntaxProvider.CreateSyntaxProvider(selectClasses, extractCandidates)
                       .Where(c => c != null);

            IncrementalValuesProvider<GeneratorClassCandidate> distinctCandidates =
                candidateClasses.Collect().SelectMany((c, _) => c.Distinct());

            context.RegisterImplementationSourceOutput(distinctCandidates, emit);
        }

        private bool selectClasses(SyntaxNode syntaxNode, CancellationToken cancellationToken)
            => GeneratorClassCandidate.IsCandidate(syntaxNode);

        private GeneratorClassCandidate extractCandidates(GeneratorSyntaxContext context, CancellationToken cancellationToken)
            => GeneratorClassCandidate.TryCreate(context.Node, context.SemanticModel)!;

        private void emit(SourceProductionContext context, GeneratorClassCandidate candidate)
            => new DependenciesFileEmitter(candidate).Emit(context.AddSource);
    }
}
