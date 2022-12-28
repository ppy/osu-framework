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
    public class DrawableCodeFixProvider : CodeFixProvider
    {
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            SyntaxNode? node = root?.FindToken(diagnosticSpan.Start).Parent;

            if (node == null)
                throw new InvalidOperationException($"Making class partial failed (null syntax) at: {diagnostic.Location}");

            switch (node)
            {
                case AttributeListSyntax:
                {
                    ClassDeclarationSyntax? classSyntax = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();

                    if (classSyntax == null)
                        break;

                    if (registerCodeFixForClass(context, classSyntax, diagnostic))
                        return;

                    break;
                }

                case ClassDeclarationSyntax classSyntax:
                {
                    if (registerCodeFixForClass(context, classSyntax, diagnostic))
                        return;

                    break;
                }

                case TypeSyntax typeSyntax:
                {
                    if (await registerCodeFixForType(context, typeSyntax, diagnostic).ConfigureAwait(false))
                        return;

                    break;
                }

                case ObjectCreationExpressionSyntax objectCreationExpression:
                {
                    if (await registerCodeFixForType(context, objectCreationExpression.Type.Parent!, diagnostic).ConfigureAwait(false))
                        return;

                    break;
                }

                case ExpressionSyntax expressionSyntax:
                {
                    if (await registerCodeFixForType(context, expressionSyntax.Parent!, diagnostic).ConfigureAwait(false))
                        return;

                    break;
                }
            }

            throw new InvalidOperationException($"Making class partial failed (non-matching node) at: {diagnostic.Location} ({node.GetType()}");
        }

        private bool registerCodeFixForClass(CodeFixContext context, ClassDeclarationSyntax classSyntax, Diagnostic diagnostic)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Make class '{classSyntax.Identifier.ValueText}' partial",
                    cancellationToken => createChangedSolution(context.Document, classSyntax, cancellationToken),
                    DiagnosticRules.MAKE_DI_CLASS_PARTIAL.Id),
                diagnostic);

            return true;
        }

        private async Task<bool> registerCodeFixForType(CodeFixContext context, SyntaxNode typeSyntax, Diagnostic diagnostic)
        {
            var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);

            ITypeSymbol? typeSymbol = compilation?.GetSemanticModel(typeSyntax.SyntaxTree).GetTypeInfo(typeSyntax).Type;

            if (typeSymbol == null)
                return false;

            if (typeSymbol.DeclaringSyntaxReferences.Length == 0)
                return false;

            ClassDeclarationSyntax? classSyntax = (await typeSymbol.DeclaringSyntaxReferences[0].SyntaxTree.GetRootAsync(context.CancellationToken).ConfigureAwait(false))
                                                  .DescendantNodes().OfType<ClassDeclarationSyntax>()
                                                  .FirstOrDefault(c => c.Identifier.ValueText == typeSymbol.Name);

            if (classSyntax == null)
                return false;

            context.RegisterCodeFix(
                CodeAction.Create(
                    $"Make class '{typeSymbol.Name}' partial",
                    cancellationToken => createChangedSolution(context.Document, classSyntax, cancellationToken),
                    DiagnosticRules.MAKE_DI_CLASS_PARTIAL.Id),
                diagnostic);

            return true;
        }

        private async Task<Solution> createChangedSolution(Document document, ClassDeclarationSyntax classSyntax, CancellationToken cancellationtoken)
        {
            Document? classDocument = document.Project.Solution.GetDocument(classSyntax.SyntaxTree);

            if (classDocument == null)
                return document.Project.Solution;

            SyntaxNode? rootNode = await classDocument.GetSyntaxRootAsync(cancellationtoken).ConfigureAwait(false);

            if (rootNode == null)
                return document.Project.Solution;

            ClassDeclarationSyntax[] toReplace = classSyntax.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().ToArray();
            rootNode = rootNode.TrackNodes(toReplace.OfType<SyntaxNode>());

            foreach (var target in toReplace)
            {
                if (target.Modifiers.Any(SyntaxKind.PartialKeyword))
                    continue;

                var currentNode = rootNode.GetCurrentNode(target)!;

                rootNode = rootNode.ReplaceNode(
                    currentNode,
                    currentNode.WithModifiers(new SyntaxTokenList(target.Modifiers)
                        .Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword))));
            }

            return classDocument.Project.Solution.WithDocumentSyntaxRoot(classDocument.Id, rootNode);
        }

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL.Id);
    }
}
