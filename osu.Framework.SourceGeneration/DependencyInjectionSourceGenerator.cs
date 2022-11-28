// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        {
            if (syntaxNode is not ClassDeclarationSyntax classSyntax)
                return false;

            if (classSyntax.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return false;

            if (classSyntax.BaseList == null && classSyntax.AttributeLists.Count == 0)
                return false;

            return true;
        }

        private GeneratorClassCandidate extractCandidates(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            ClassDeclarationSyntax classSyntax = (ClassDeclarationSyntax)context.Node;
            INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);

            if (symbol == null)
                return null!;

            // Determine if the class is a candidate for the source generator.
            if (!symbol.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface))
                return null!;

            return new GeneratorClassCandidate(symbol);
        }

        private void emit(SourceProductionContext context, GeneratorClassCandidate candidate)
        {
            // Fully qualified name, with generics replaced with friendly characters.
            string typeName = candidate.FullyQualifiedTypeName.Replace('<', '{').Replace('>', '}');
            string filename = $"g_{typeName}_Dependencies.cs";

            context.AddSource(filename, new DependenciesFileEmitter(candidate).Emit());
        }
    }
}
