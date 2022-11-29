// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    [Generator]
    public class DependencyInjectionSourceGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new CustomSyntaxContextReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not CustomSyntaxContextReceiver receiver)
                return;

            foreach (var candidate in receiver.Candidates.Distinct())
            {
                // Fully qualified name, with generics replaced with friendly characters.
                string typeName = candidate.FullyQualifiedTypeName.Replace('<', '{').Replace('>', '}');
                string filename = $"g_{typeName}_Dependencies.cs";

                context.AddSource(filename, new DependenciesFileEmitter(candidate).Emit());
            }
        }

        private class CustomSyntaxContextReceiver : ISyntaxContextReceiver
        {
            public readonly List<GeneratorClassCandidate> Candidates = new List<GeneratorClassCandidate>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is not ClassDeclarationSyntax classSyntax)
                    return;

                if (classSyntax.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
                    return;

                if (classSyntax.BaseList == null && classSyntax.AttributeLists.Count == 0)
                    return;

                INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);

                if (symbol == null)
                    return;

                // Determine if the class is a candidate for the source generator.
                if (!symbol.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface))
                    return;

                Candidates.Add(new GeneratorClassCandidate(symbol));
            }
        }
    }
}
