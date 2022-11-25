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
            context.RegisterSyntaxNodeAction(analyseInvocation, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(analyseObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        /// <summary>
        /// Analyses construction of CachedModelDependencyContainer{T}.
        /// </summary>
        private void analyseObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreationSyntax = (ObjectCreationExpressionSyntax)context.Node;

            GenericNameSyntax? genericName = objectCreationSyntax.Type as GenericNameSyntax;

            if (objectCreationSyntax.Type is QualifiedNameSyntax qualified)
                genericName = qualified.Right as GenericNameSyntax;

            if (genericName == null)
                return;

            if (genericName.Identifier.ValueText != "CachedModelDependencyContainer")
                return;

            TypeSyntax? typeSyntax = genericName.TypeArgumentList.Arguments.FirstOrDefault();

            if (typeSyntax == null)
                return;

            ITypeSymbol? argumentType = context.SemanticModel.GetTypeInfo(typeSyntax).Type;
            SyntaxTree? argumentSyntaxTree = argumentType?.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;
            ClassDeclarationSyntax? argumentClassSyntax = argumentSyntaxTree?.GetRoot().DescendantNodesAndSelf()
                                                                            .OfType<ClassDeclarationSyntax>()
                                                                            .FirstOrDefault(c => c.Identifier.ValueText == argumentType?.Name);

            if (argumentClassSyntax == null)
                return;

            if (argumentClassSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                return;

            // Todo: Why doesn't this work for nested class? It _is_ getting here...
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL, typeSyntax.GetLocation(), typeSyntax));
        }

        /// <summary>
        /// Analyses invocations of DependencyContainer.Inject{T}(T obj).
        /// </summary>
        private void analyseInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocationSyntax = (InvocationExpressionSyntax)context.Node;

            if (invocationSyntax.ArgumentList.Arguments.Count == 0)
                return;

            if (invocationSyntax.Expression is not MemberAccessExpressionSyntax memberAccessSyntax)
                return;

            if (memberAccessSyntax.Name.Identifier.ValueText != "Inject")
                return;

            ITypeSymbol? expressionType = context.SemanticModel.GetTypeInfo(memberAccessSyntax.Expression).Type;

            if (expressionType == null)
                return;

            if (!SyntaxHelpers.IsIReadOnlyDependencyContainerInterface(expressionType) && !expressionType.AllInterfaces.Any(SyntaxHelpers.IsIReadOnlyDependencyContainerInterface))
                return;

            ExpressionSyntax argumentExpression = invocationSyntax.ArgumentList.Arguments[0].Expression;
            ITypeSymbol? argumentType = context.SemanticModel.GetTypeInfo(argumentExpression).Type;
            SyntaxTree? argumentSyntaxTree = argumentType?.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;
            ClassDeclarationSyntax? argumentClassSyntax = argumentSyntaxTree?.GetRoot().DescendantNodesAndSelf()
                                                                            .OfType<ClassDeclarationSyntax>()
                                                                            .FirstOrDefault(c => c.Identifier.ValueText == argumentType?.Name);

            if (argumentClassSyntax == null)
                return;

            if (argumentClassSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                return;

            // Todo: Why doesn't this work for nested class? It _is_ getting here...
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL, argumentExpression.GetLocation(), argumentExpression));
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

            if (type != null && requiresPartialClass(type))
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticRules.MAKE_DI_CLASS_PARTIAL, context.Node.GetLocation(), context.Node));
        }

        private bool requiresPartialClass(ITypeSymbol type)
        {
            // "Transformable" is a special class below "Drawable" but still a part of the drawable hierarchy.
            // It's the base-most type of all drawable objects, and so needs to be partial (see below).
            if (SyntaxHelpers.IsTransformableType(type))
                return true;

            // "IDrawable" classes need to be partial since dependency injection happens implicitly through the drawable hierarchy.
            if (type.AllInterfaces.Any(SyntaxHelpers.IsIDrawableInterface))
                return true;

            // "ISourceGeneratedDependencyActivatorInterface" classes need to be partial since their base type is used in dependency injection.
            if (type.AllInterfaces.Any(SyntaxHelpers.IsISourceGeneratedDependencyActivatorInterface))
                return true;

            return false;
        }
    }
}
