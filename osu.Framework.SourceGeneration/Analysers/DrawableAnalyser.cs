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
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticRules.MAKE_DI_CLASS_PARTIAL,
            DiagnosticRules.ASYNC_BDL_MUST_RETURN_TASK);

        public override void Initialize(AnalysisContext context)
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(analyseClass, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(analyseMethod, SyntaxKind.MethodDeclaration);
        }

        /// <summary>
        /// Analyses class definitions for implementations of IDrawable, ISourceGeneratedDependencyActivator, and Transformable.
        /// </summary>
        private void analyseClass(SyntaxNodeAnalysisContext context)
        {
            var classSyntax = (ClassDeclarationSyntax)context.Node;

            if (classSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return;

            INamedTypeSymbol? type = context.SemanticModel.GetDeclaredSymbol(classSyntax);

            if (type?.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface) == true)
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL, context.Node.GetLocation(), context.Node));
        }

        private void analyseMethod(SyntaxNodeAnalysisContext context)
        {
            var methodSyntax = (MethodDeclarationSyntax)context.Node;

            // Filter out methods that aren't async.
            if (methodSyntax.Modifiers.All(m => !m.IsKind(SyntaxKind.AsyncKeyword)))
                return;

            // Filter out methods that don't return void.
            if (methodSyntax.ReturnType.ToString() != "void")
                return;

            // We expect the number of async void-returning methods to be pretty minimal, so retrieving symbols shouldn't be too expensive...
            IMethodSymbol? method = context.SemanticModel.GetDeclaredSymbol(methodSyntax);

            if (method?.GetAttributes().Any(SyntaxHelpers.IsBackgroundDependencyLoaderAttribute) == true)
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ASYNC_BDL_MUST_RETURN_TASK, context.Node.GetLocation()));
        }
    }
}
