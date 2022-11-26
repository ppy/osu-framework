// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Collections.Immutable;
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

            IncrementalValueProvider<(Compilation Compilation, ImmutableArray<GeneratorClassCandidate> classes)> compilationAndClasses =
                context.CompilationProvider.Combine(candidateClasses.Collect());

            context.RegisterImplementationSourceOutput(compilationAndClasses, emit);
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

            GeneratorClassCandidate candidate = new GeneratorClassCandidate(classSyntax, symbol);

            // Process any [Cached] attributes on any interface on the class excluding base types.
            foreach (var iFace in SyntaxHelpers.GetDeclaredInterfacesOnType(symbol))
            {
                // Add an entry if this interface has a cached attribute.
                if (iFace.GetAttributes().Any(attrib => SyntaxHelpers.IsCachedAttribute(attrib.AttributeClass)))
                    candidate.CachedInterfaces.Add(iFace);
            }

            // Process any [Cached] attributes on the class.
            foreach (var attrib in enumerateAttributes(context.SemanticModel, classSyntax))
            {
                if (SyntaxHelpers.IsCachedAttribute(context.SemanticModel, attrib))
                    candidate.CachedClasses.Add(new SyntaxWithSymbol(context, classSyntax));
            }

            // Process any attributes of members of the class.
            foreach (var member in classSyntax.Members)
            {
                foreach (var attrib in enumerateAttributes(context.SemanticModel, member))
                {
                    if (SyntaxHelpers.IsBackgroundDependencyLoaderAttribute(context.SemanticModel, attrib))
                        candidate.DependencyLoaderMemebers.Add(new SyntaxWithSymbol(context, member));

                    if (member is not PropertyDeclarationSyntax && member is not FieldDeclarationSyntax)
                        continue;

                    if (SyntaxHelpers.IsResolvedAttribute(context.SemanticModel, attrib))
                        candidate.ResolvedMembers.Add(new SyntaxWithSymbol(context, member));

                    if (SyntaxHelpers.IsCachedAttribute(context.SemanticModel, attrib))
                        candidate.CachedMembers.Add(new SyntaxWithSymbol(context, member));
                }
            }

            return candidate;
        }

        private static IEnumerable<AttributeSyntax> enumerateAttributes(SemanticModel semanticModel, MemberDeclarationSyntax member)
        {
            return member.AttributeLists
                         .SelectMany(attribList =>
                             attribList.Attributes
                                       .Where(attrib =>
                                           SyntaxHelpers.IsBackgroundDependencyLoaderAttribute(semanticModel, attrib)
                                           || SyntaxHelpers.IsResolvedAttribute(semanticModel, attrib)
                                           || SyntaxHelpers.IsCachedAttribute(semanticModel, attrib)));
        }

        private void emit(SourceProductionContext context, (Compilation compilation, ImmutableArray<GeneratorClassCandidate> candidates) items)
        {
            if (items.candidates.IsDefaultOrEmpty)
                return;

            IEnumerable<GeneratorClassCandidate> distinctCandidates = items.candidates.Distinct();

            foreach (var candidate in distinctCandidates)
            {
                // Fully qualified name, with generics replaced with friendly characters.
                string typeName = candidate.Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)
                                           .Replace('<', '{')
                                           .Replace('>', '}');

                string filename = $"g_{typeName}_Dependencies.cs";

                context.AddSource(filename, new DependenciesFileEmitter(candidate, items.compilation).Emit());
            }
        }
    }
}
