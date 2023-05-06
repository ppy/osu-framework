// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
            DiagnosticRules.ASYNC_BDL_MUST_RETURN_TASK,
            DiagnosticRules.CONFIGURE_AWAIT_MUST_BE_TRUE);

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

            // Lazy filter to BDL-attributed methods.
            if (!methodSyntax.AttributeLists.SelectMany(l => l.Attributes).Any(a => a.Name.ToString().Contains("BackgroundDependencyLoader")))
                return;

            IMethodSymbol? method = context.SemanticModel.GetDeclaredSymbol(methodSyntax);

            // Semantic attribute check
            if (method?.GetAttributes().Any(SyntaxHelpers.IsBackgroundDependencyLoaderAttribute) != true)
                return;

            analyseBDLAsyncSignature(methodSyntax, context);
            analyseBDLAwaitInvocations(methodSyntax, context);
        }

        /// <summary>
        /// Analyses an async BDL method to ensure it returns <see cref="Task"/>.
        /// </summary>
        /// <param name="methodSyntax">The BDL method.</param>
        /// <param name="context">The analysis context.</param>
        private void analyseBDLAsyncSignature(MethodDeclarationSyntax methodSyntax, SyntaxNodeAnalysisContext context)
        {
            if (methodSyntax.ReturnType.ToString() == "void")
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.ASYNC_BDL_MUST_RETURN_TASK, context.Node.GetLocation()));
        }

        /// <summary>
        /// Analyses an async BDL method to ensure all immediate awaited methods continue on the async load context.
        /// </summary>
        /// <param name="methodSyntax">The BDL method.</param>
        /// <param name="context">The analysis context.</param>
        private void analyseBDLAwaitInvocations(MethodDeclarationSyntax methodSyntax, SyntaxNodeAnalysisContext context)
        {
            foreach (var awaitExp in methodSyntax.DescendantNodes(n => !isFunctionDelegate(n)).OfType<AwaitExpressionSyntax>())
            {
                if (awaitExp.Expression is not InvocationExpressionSyntax invocationExp
                    || invocationExp.Expression is not MemberAccessExpressionSyntax memberAccess
                    || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                    || memberAccess.Name.ToString() != "ConfigureAwait")
                {
                    continue;
                }

                if (invocationExp.ArgumentList.Arguments.SingleOrDefault()?.Expression is not LiteralExpressionSyntax arg
                    || !arg.IsKind(SyntaxKind.TrueLiteralExpression))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.CONFIGURE_AWAIT_MUST_BE_TRUE, invocationExp.ArgumentList.GetLocation()));
                }
            }

            static bool isFunctionDelegate(SyntaxNode node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.LocalFunctionStatement:
                    case SyntaxKind.ArrowExpressionClause:
                    case SyntaxKind.DelegateDeclaration:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        return true;
                }

                return false;
            }
        }
    }
}
