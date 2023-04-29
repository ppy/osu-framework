// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.UnbindAllBindables
{
    public class UnbindAllBindablesSourceEmitter : IncrementalSourceEmitter
    {
        private const string unbind_all_bindables_method_name = "InternalUnbindAllBindables";
        private const string interface_name = "global::osu.Framework.Graphics.ISourceGeneratedUnbindAllBindables";

        protected override string FileSuffix => "UnbindAllBindables";

        public new UnbindAllBindablesSemanticTarget Target => (UnbindAllBindablesSemanticTarget)base.Target;

        public UnbindAllBindablesSourceEmitter(IncrementalSemanticTarget target)
            : base(target)
        {
        }

        protected override ClassDeclarationSyntax ConstructClass(ClassDeclarationSyntax initialClass)
        {
            return initialClass.WithBaseList(
                                   SyntaxFactory.BaseList(
                                       SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                           SyntaxFactory.SimpleBaseType(
                                               SyntaxFactory.ParseTypeName(interface_name)))))
                               .WithMembers(
                                   SyntaxFactory.List(new[]
                                   {
                                       emitKnownType(),
                                       emitUnbindAllBindables()
                                   }));
        }

        private MemberDeclarationSyntax emitKnownType()
        {
            return SyntaxFactory.PropertyDeclaration(
                                    SyntaxFactory.ParseTypeName("global::System.Type"),
                                    SyntaxFactory.Identifier("KnownType"))
                                .WithExplicitInterfaceSpecifier(
                                    SyntaxFactory.ExplicitInterfaceSpecifier(
                                        SyntaxFactory.IdentifierName(interface_name)))
                                .WithExpressionBody(
                                    SyntaxFactory.ArrowExpressionClause(
                                        SyntaxFactory.TypeOfExpression(
                                            SyntaxFactory.ParseTypeName(Target.GlobalPrefixedTypeName))))
                                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }

        private MemberDeclarationSyntax emitUnbindAllBindables()
        {
            return SyntaxFactory.MethodDeclaration(
                                    SyntaxFactory.PredefinedType(
                                        SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                    unbind_all_bindables_method_name)
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                        Target.NeedsOverride
                                            ? SyntaxFactory.Token(SyntaxKind.OverrideKeyword)
                                            : SyntaxFactory.Token(SyntaxKind.VirtualKeyword)))
                                .WithBody(
                                    SyntaxFactory.Block(emitStatements()));

            IEnumerable<StatementSyntax> emitStatements()
            {
                if (Target.NeedsOverride)
                {
                    yield return SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.BaseExpression(),
                                SyntaxFactory.IdentifierName(unbind_all_bindables_method_name))));
                }

                foreach (BindableDefinition bindable in Target.Bindables)
                {
                    yield return SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                                         SyntaxFactory.MemberAccessExpression(
                                             SyntaxKind.SimpleMemberAccessExpression,
                                             SyntaxFactory.ParseTypeName("global::osu.Framework.Utils.SourceGeneratorUtils"),
                                             SyntaxFactory.IdentifierName("UnbindBindable")))
                                     .WithArgumentList(
                                         SyntaxFactory.ArgumentList(
                                             SyntaxFactory.SeparatedList(new[]
                                             {
                                                 SyntaxFactory.Argument(
                                                     SyntaxHelpers.TypeOf(Target.GlobalPrefixedTypeName)),
                                                 SyntaxFactory.Argument(
                                                     SyntaxFactory.MemberAccessExpression(
                                                         SyntaxKind.SimpleMemberAccessExpression,
                                                         SyntaxFactory.ParenthesizedExpression(
                                                             SyntaxFactory.CastExpression(
                                                                 SyntaxFactory.IdentifierName(bindable.FullyQualifiedContainingType),
                                                                 SyntaxFactory.ThisExpression())),
                                                         SyntaxFactory.IdentifierName(bindable.Name)))
                                             }))));
                }
            }
        }
    }
}
