// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Emitters
{
    public class DependenciesFileEmitter
    {
        public const string ACTIVATOR_PARAMETER_NAME = "activator";

        public const string IS_REGISTERED_METHOD_NAME = "IsRegistered";
        public const string REGISTER_DEPENDENCY_ACTIVATOR_METHOD_NAME = "RegisterDependencyActivator";
        public const string REGISTER_METHOD_NAME = "Register";

        public const string TARGET_PARAMETER_NAME = "t";
        public const string DEPENDENCIES_PARAMETER_NAME = "d";
        public const string CACHE_INFO_PARAMETER_NAME = "i";

        public const string LOCAL_DEPENDENCIES_VAR_NAME = "dependencies";

        public readonly GeneratorExecutionContext Context;
        public readonly SyntaxContextReceiver Receiver;
        public readonly GeneratorClassCandidate Candidate;

        public readonly ITypeSymbol ClassType;
        public readonly INamedTypeSymbol? CachedAttributeType;
        public readonly INamedTypeSymbol? ResolvedAttributeType;
        public readonly INamedTypeSymbol? BackgroundDependencyLoaderAttributeType;
        public readonly INamedTypeSymbol? BindableTypeSymbol;

        private readonly ITypeSymbol iSourceGeneratedDependencyActivatorType;
        private readonly ITypeSymbol iDependencyActivatorType;
        private readonly bool needsOverride;

        public DependenciesFileEmitter(GeneratorExecutionContext context, SyntaxContextReceiver receiver, GeneratorClassCandidate candidate)
        {
            Context = context;
            Receiver = receiver;
            Candidate = candidate;

            ClassType = (ITypeSymbol)ModelExtensions.GetDeclaredSymbol(context.Compilation.GetSemanticModel(candidate.ClassSyntax.SyntaxTree), candidate.ClassSyntax)!;
            CachedAttributeType = context.Compilation.GetTypeByMetadataName("osu.Framework.Allocation.CachedAttribute");
            ResolvedAttributeType = context.Compilation.GetTypeByMetadataName("osu.Framework.Allocation.ResolvedAttribute");
            BackgroundDependencyLoaderAttributeType = context.Compilation.GetTypeByMetadataName("osu.Framework.Allocation.BackgroundDependencyLoaderAttribute");
            BindableTypeSymbol = context.Compilation.GetTypeByMetadataName("osu.Framework.Bindables.IBindable");
            iSourceGeneratedDependencyActivatorType = context.Compilation.GetTypeByMetadataName("osu.Framework.Allocation.ISourceGeneratedDependencyActivator")!;
            iDependencyActivatorType = context.Compilation.GetTypeByMetadataName("osu.Framework.Allocation.IDependencyActivator")!;

            needsOverride =
                // Override necessary if the class already has the source generator interface name.
                candidate.Symbol.AllInterfaces.Any(SyntaxHelpers.IsISourceGeneratedDependencyActivatorInterface)
                // Or if any base types are to be processed by this generator.
                || SyntaxHelpers.EnumerateBaseTypes(candidate.Symbol).Any(t => receiver.CandidateClasses.ContainsKey(t));
        }

        public string Emit()
        {
            if (ClassType.ContainingNamespace.IsGlobalNamespace)
            {
                return emitDependenciesClass()
                       .NormalizeWhitespace()
                       .ToString();
            }

            return SyntaxFactory.NamespaceDeclaration(
                                    SyntaxFactory.IdentifierName(
                                        ClassType.ContainingNamespace.ToDisplayString()))
                                .WithMembers(
                                    SyntaxFactory.SingletonList(
                                        emitDependenciesClass()))
                                .NormalizeWhitespace()
                                .ToString();
        }

        private MemberDeclarationSyntax emitDependenciesClass()
        {
            return emitTypeTree(
                cls =>
                    cls.WithBaseList(
                           SyntaxFactory.BaseList(
                               SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                   SyntaxFactory.SimpleBaseType(
                                       SyntaxFactory.ParseTypeName(iSourceGeneratedDependencyActivatorType.ToDisplayString())))))
                       .WithMembers(
                           SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                               SyntaxFactory.MethodDeclaration(
                                                SyntaxFactory.PredefinedType(
                                                    SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                                REGISTER_DEPENDENCY_ACTIVATOR_METHOD_NAME)
                                            .WithModifiers(
                                                emitMethodModifiers())
                                            .WithParameterList(
                                                SyntaxFactory.ParameterList(
                                                    SyntaxFactory.SingletonSeparatedList(
                                                        SyntaxFactory.Parameter(
                                                                         SyntaxFactory.Identifier(ACTIVATOR_PARAMETER_NAME))
                                                                     .WithType(
                                                                         SyntaxFactory.ParseTypeName(iDependencyActivatorType.ToDisplayString())))))
                                            .WithBody(
                                                SyntaxFactory.Block(
                                                    emitPrecondition(),
                                                    emitBaseCall(),
                                                    emitRegistration())))));
        }

        private ClassDeclarationSyntax emitTypeTree(Func<ClassDeclarationSyntax, ClassDeclarationSyntax> innerClassAction)
        {
            List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();

            ITypeSymbol? typeSymbol = ClassType;

            while (typeSymbol != null)
            {
                classes.Add(createClassSyntax(typeSymbol));
                typeSymbol = typeSymbol.ContainingType ?? null;
            }

            classes[0] = innerClassAction(classes[0]);

            for (int i = 0; i < classes.Count - 1; i++)
                classes[i + 1] = classes[i + 1].WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[] { classes[i] }));

            return classes.Last();

            static ClassDeclarationSyntax createClassSyntax(ITypeSymbol typeSymbol)
            {
                string name = typeSymbol.Name;

                if (typeSymbol is INamedTypeSymbol named && named.TypeParameters.Length > 0)
                    name += $@"<{string.Join(@", ", named.TypeParameters)}>";

                return SyntaxFactory.ClassDeclaration(name)
                                    .WithModifiers(
                                        SyntaxTokenList.Create(
                                            SyntaxFactory.Token(SyntaxKind.PartialKeyword)));
            }
        }

        private SyntaxTokenList emitMethodModifiers()
        {
            if (needsOverride)
            {
                return SyntaxFactory.TokenList(
                    SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                    SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            }

            return SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
        }

        private StatementSyntax emitPrecondition()
        {
            return SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                                 SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     SyntaxFactory.IdentifierName(ACTIVATOR_PARAMETER_NAME),
                                     SyntaxFactory.IdentifierName(IS_REGISTERED_METHOD_NAME)))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.Argument(
                                             SyntaxFactory.TypeOfExpression(
                                                 SyntaxFactory.ParseTypeName(ClassType.ToDisplayString())))))),
                SyntaxFactory.ReturnStatement());
        }

        private StatementSyntax emitBaseCall()
        {
            if (!needsOverride)
                return SyntaxFactory.ParseStatement(string.Empty);

            return SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                                     SyntaxFactory.MemberAccessExpression(
                                                         SyntaxKind.SimpleMemberAccessExpression,
                                                         SyntaxFactory.BaseExpression(),
                                                         SyntaxFactory.IdentifierName(REGISTER_DEPENDENCY_ACTIVATOR_METHOD_NAME)))
                                                 .WithArgumentList(
                                                     SyntaxFactory.ArgumentList(
                                                         SyntaxFactory.SingletonSeparatedList(
                                                             SyntaxFactory.Argument(
                                                                 SyntaxFactory.IdentifierName(ACTIVATOR_PARAMETER_NAME))))))
                                .WithLeadingTrivia(
                                    SyntaxFactory.TriviaList(
                                        SyntaxFactory.LineFeed));
        }

        private StatementSyntax emitRegistration()
        {
            return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                                 SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     SyntaxFactory.IdentifierName(ACTIVATOR_PARAMETER_NAME),
                                     SyntaxFactory.IdentifierName(REGISTER_METHOD_NAME)))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList(
                                     SyntaxFactory.SeparatedList(new[]
                                     {
                                         SyntaxFactory.Argument(
                                             SyntaxFactory.TypeOfExpression(
                                                 SyntaxFactory.ParseTypeName(ClassType.ToDisplayString()))),
                                         SyntaxFactory.Argument(
                                             emitInjectDependenciesDelegate()),
                                         SyntaxFactory.Argument(
                                             emitCacheDependenciesDelegate())
                                     }))));
        }

        private ExpressionSyntax emitInjectDependenciesDelegate()
        {
            if (Candidate.DependencyLoaderMemebers.Count == 0 && Candidate.ResolvedMembers.Count == 0)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            return SyntaxFactory.ParenthesizedLambdaExpression()
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(TARGET_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(DEPENDENCIES_PARAMETER_NAME)),
                                        })))
                                .WithBlock(
                                    SyntaxFactory.Block(
                                        Candidate.ResolvedMembers.Select(m => (IStatementEmitter)new ResolvedMemberEmitter(this, m))
                                                 .Concat(
                                                     Candidate.DependencyLoaderMemebers.Select(m => new BackgroundDependencyLoaderEmitter(this, m)))
                                                 .SelectMany(
                                                     e => e.Emit())));
        }

        private ExpressionSyntax emitCacheDependenciesDelegate()
        {
            if (Candidate.CachedMembers.Count == 0 && Candidate.CachedInterfaces.Count == 0 && Candidate.CachedClasses.Count == 0)
                return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);

            return SyntaxFactory.ParenthesizedLambdaExpression()
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(TARGET_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(DEPENDENCIES_PARAMETER_NAME)),
                                            SyntaxFactory.Parameter(
                                                SyntaxFactory.Identifier(CACHE_INFO_PARAMETER_NAME))
                                        })))
                                .WithBlock(
                                    SyntaxFactory.Block(
                                        Candidate.CachedMembers.Select(m => (IStatementEmitter)new CachedMemberEmitter(this, m))
                                                 .Concat(
                                                     Candidate.CachedClasses.Select(m => new CachedClassEmitter(this, m)))
                                                 .Concat(
                                                     Candidate.CachedInterfaces.Select(m => new CachedInterfaceEmitter(this, m)))
                                                 .SelectMany(
                                                     e => e.Emit())
                                                 .Prepend(createPrologue())
                                                 .Append(createEpilogue())));

            static StatementSyntax createPrologue() =>
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var"))
                                 .WithVariables(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.VariableDeclarator(
                                                          SyntaxFactory.Identifier(LOCAL_DEPENDENCIES_VAR_NAME))
                                                      .WithInitializer(
                                                          SyntaxFactory.EqualsValueClause(
                                                              SyntaxFactory.ObjectCreationExpression(
                                                                               SyntaxFactory.ParseTypeName("osu.Framework.Allocation.DependencyContainer"))
                                                                           .WithArgumentList(
                                                                               SyntaxFactory.ArgumentList(
                                                                                   SyntaxFactory.SingletonSeparatedList(
                                                                                       SyntaxFactory.Argument(
                                                                                           SyntaxFactory.IdentifierName(DEPENDENCIES_PARAMETER_NAME))))))))));

            static StatementSyntax createEpilogue() =>
                SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(LOCAL_DEPENDENCIES_VAR_NAME));
        }
    }
}
