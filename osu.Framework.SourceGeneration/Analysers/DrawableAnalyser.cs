// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace osu.Framework.SourceGeneration.Analysers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DrawableAnalyser : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(analyseClass, SyntaxKind.ClassDeclaration);
        }

        /// <summary>
        /// Analyses class definitions for implementations of IDependencyInjectionCandidateInterface.
        /// </summary>
        private void analyseClass(SyntaxNodeAnalysisContext context)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;

            if (classSyntax.Ancestors().OfType<ClassDeclarationSyntax>().Any())
                return;

            analyseRecursively(context, classSyntax);

            static bool analyseRecursively(SyntaxNodeAnalysisContext context, ClassDeclarationSyntax node)
            {
                bool requiresPartial = false;

                // Child nodes always have to be analysed to provide diagnostics.
                foreach (var nested in node.DescendantNodes().OfType<ClassDeclarationSyntax>())
                    requiresPartial |= analyseRecursively(context, nested);

                // - If at least one child requires partial, then this node also needs to be partial regardless of its own type (optimisation).
                // - If no child requires partial, we need to check if this node is a DI candidate (e.g. If the node has no nested types).
                if (!requiresPartial)
                    requiresPartial = context.SemanticModel.GetDeclaredSymbol(node)?.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface) == true;

                // Whether the node is already partial.
                bool isPartial = node.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

                if (requiresPartial && !isPartial)
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL, node.GetLocation(), node));

                return requiresPartial;
            }
        }
    }
}
