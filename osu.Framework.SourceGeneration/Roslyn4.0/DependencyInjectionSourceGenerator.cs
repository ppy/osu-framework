// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    [Generator]
    public class DependencyInjectionSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<GeneratorClassCandidate> syntaxTargets =
                context.SyntaxProvider.CreateSyntaxProvider(
                    (n, _) => GeneratorClassCandidate.IsSyntaxTarget(n),
                    (ctx, _) => new GeneratorClassCandidate((ClassDeclarationSyntax)ctx.Node, ctx.SemanticModel));

            IncrementalValuesProvider<GeneratorClassCandidate> semanticTargets =
                syntaxTargets
                    .Select((c, _) => c.GetSemanticTarget());

            IncrementalValuesProvider<GeneratorClassCandidate> distinctCandidates =
                semanticTargets.Collect().SelectMany((c, _) => c.Distinct());

            context.RegisterImplementationSourceOutput(distinctCandidates, emit);
        }

        private void emit(SourceProductionContext context, GeneratorClassCandidate candidate)
            => new DependenciesFileEmitter(candidate).Emit(context.AddSource);
    }
}
