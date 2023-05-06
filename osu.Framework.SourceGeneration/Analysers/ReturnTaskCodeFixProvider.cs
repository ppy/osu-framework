// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Analysers
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class ReturnTaskCodeFixProvider : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            SyntaxNode? node = root?.FindToken(diagnosticSpan.Start).Parent;

            if (node == null)
                throw new InvalidOperationException($"Making BDL method return task failed (null syntax) at: {diagnostic.Location}");

            MethodDeclarationSyntax? methodSyntax = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (methodSyntax == null)
                throw new InvalidOperationException($"Making BDL method return task failed (non-matching node) at: {diagnostic.Location} ({node.GetType()})");

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Make method '{methodSyntax.Identifier.ValueText}' return Task",
                    cancellationToken => createChangedSolution(context.Document, methodSyntax, cancellationToken),
                    DiagnosticRules.ASYNC_BDL_MUST_RETURN_TASK.Id),
                diagnostic);
        }

        private async Task<Solution> createChangedSolution(Document document, MethodDeclarationSyntax methodSyntax, CancellationToken cancellationtoken)
        {
            SyntaxNode? rootNode = await document.GetSyntaxRootAsync(cancellationtoken).ConfigureAwait(false);

            if (rootNode == null)
                return document.Project.Solution;

            rootNode = rootNode.ReplaceNode(
                methodSyntax,
                methodSyntax.WithReturnType(
                    SyntaxFactory.ParseTypeName("Task ")));

            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, rootNode);
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticRules.ASYNC_BDL_MUST_RETURN_TASK.Id);
    }
}
