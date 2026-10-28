// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.Dependencies.Emitters
{
    public class DependenciesFileEmitter : IncrementalSourceEmitter
    {
        public const string REGISTRY_PARAMETER_NAME = "registry";

        public const string IS_REGISTERED_METHOD_NAME = "IsRegistered";
        public const string REGISTER_FOR_DEPENDENCY_ACTIVATION_METHOD_NAME = "RegisterForDependencyActivation";
        public const string REGISTER_METHOD_NAME = "Register";

        public const string TARGET_PARAMETER_NAME = "t";
        public const string DEPENDENCIES_PARAMETER_NAME = "d";
        public const string CACHE_INFO_PARAMETER_NAME = "i";

        public const string LOCAL_DEPENDENCIES_VAR_NAME = "dependencies";

        protected override string FileSuffix => "Dependencies";

        public new DependenciesClassCandidate Target => (DependenciesClassCandidate)base.Target;

        public DependenciesFileEmitter(IncrementalSemanticTarget target)
            : base(target)
        {
        }

        protected override ClassDeclarationSyntax ConstructClass(ClassDeclarationSyntax initialClass)
        {
            return initialClass.WithBaseList(
                                   SyntaxFactory.BaseList(
                                       SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                           SyntaxFactory.SimpleBaseType(
                                               SyntaxFactory.ParseTypeName("global::osu.Framework.Allocation.ISourceGeneratedDependencyActivator")))))
                               .WithMembers(
                                   SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                       SyntaxFactory.MethodDeclaration(
                                                        SyntaxFactory.PredefinedType(
                                                            SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                                        REGISTER_FOR_DEPENDENCY_ACTIVATION_METHOD_NAME)
                                                    .WithModifiers(
                                                        SyntaxFactory.TokenList(
                                                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                            Target.NeedsOverride
                                                                ? SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
                                                                : SyntaxFactory.Token(SyntaxKind.VirtualKeyword)))
                                                    .WithParameterList(
                                                        SyntaxFactory.ParameterList(
                                                            SyntaxFactory.SingletonSeparatedList(
                                                                SyntaxFactory.Parameter(
                                                                                 SyntaxFactory.Identifier(REGISTRY_PARAMETER_NAME))
                                                                             .WithType(
                                                                                 SyntaxFactory.ParseTypeName("global::osu.Framework.Allocation.IDependencyActivatorRegistry")))))
                                                    .WithBody(
                                                        SyntaxFactory.Block(
                                                            emitPrecondition(),
                                                            emitBaseCall(),
                                                            emitRegistration()))));
        }

        private StatementSyntax emitPrecondition()
        {
            return SyntaxFactory.IfStatement(
                SyntaxFactory.InvocationExpression(
                                 SyntaxFactory.MemberAccessExpression(
                                     SyntaxKind.SimpleMemberAccessExpression,
                                     SyntaxFactory.IdentifierName(REGISTRY_PARAMETER_NAME),
                                     SyntaxFactory.IdentifierName(IS_REGISTERED_METHOD_NAME)))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList(
                                     SyntaxFactory.SingletonSeparatedList(
                                         SyntaxFactory.Argument(
                                             SyntaxFactory.TypeOfExpression(
                                                 SyntaxFactory.ParseTypeName(Target.GlobalPrefixedTypeName)))))),
                SyntaxFactory.ReturnStatement());
        }

        private StatementSyntax emitBaseCall()
        {
            if (!Target.NeedsOverride)
                return SyntaxFactory.ParseStatement(string.Empty);

            return SyntaxFactory.ExpressionStatement(
                                    SyntaxFactory.InvocationExpression(
                                                     SyntaxFactory.MemberAccessExpression(
                                                         SyntaxKind.SimpleMemberAccessExpression,
                                                         SyntaxFactory.BaseExpression(),
                                                         SyntaxFactory.IdentifierName(REGISTER_FOR_DEPENDENCY_ACTIVATION_METHOD_NAME)))
                                                 .WithArgumentList(
                                                     SyntaxFactory.ArgumentList(
                                                         SyntaxFactory.SingletonSeparatedList(
                                                             SyntaxFactory.Argument(
                                                                 SyntaxFactory.IdentifierName(REGISTRY_PARAMETER_NAME))))))
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
                                     SyntaxFactory.IdentifierName(REGISTRY_PARAMETER_NAME),
                                     SyntaxFactory.IdentifierName(REGISTER_METHOD_NAME)))
                             .WithArgumentList(
                                 SyntaxFactory.ArgumentList(
                                     SyntaxFactory.SeparatedList(new[]
                                     {
                                         SyntaxFactory.Argument(
                                             SyntaxFactory.TypeOfExpression(
                                                 SyntaxFactory.ParseTypeName(Target.GlobalPrefixedTypeName))),
                                         SyntaxFactory.Argument(
                                             emitInjectDependenciesDelegate()),
                                         SyntaxFactory.Argument(
                                             emitCacheDependenciesDelegate())
                                     }))));
        }

        private ExpressionSyntax emitInjectDependenciesDelegate()
        {
            if (Target.DependencyLoaderMembers.Count == 0 && Target.ResolvedMembers.Count == 0)
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
                                        Target.ResolvedMembers.Select(m => (IStatementEmitter)new ResolvedMemberEmitter(this, m))
                                              .Concat(Target.DependencyLoaderMembers.Select(m => new BackgroundDependencyLoaderEmitter(this, m)))
                                              .SelectMany(e => e.Emit())));
        }

        private ExpressionSyntax emitCacheDependenciesDelegate()
        {
            if (Target.CachedMembers.Count == 0 && Target.CachedInterfaces.Count == 0 && Target.CachedClasses.Count == 0)
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
                                        Target.CachedMembers.Select(m => (IStatementEmitter)new CachedMemberEmitter(this, m))
                                              .Concat(
                                                  Target.CachedClasses.Select(m => new CachedClassEmitter(this, m)))
                                              .Concat(
                                                  Target.CachedInterfaces.Select(m => new CachedInterfaceEmitter(this, m)))
                                              .SelectMany(e => e.Emit())
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
                                                                               SyntaxFactory.ParseTypeName("global::osu.Framework.Allocation.DependencyContainer"))
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
