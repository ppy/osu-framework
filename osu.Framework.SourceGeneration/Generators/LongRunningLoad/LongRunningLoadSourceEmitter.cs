// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.LongRunningLoad
{
    public class LongRunningLoadSourceEmitter : IncrementalSourceEmitter
    {
        private const string interface_name = "global::osu.Framework.Allocation.ISourceGeneratedLongRunningLoadCache";

        protected override string FileSuffix => "LongRunningLoad";

        public new LongRunningLoadSemanticTarget Target => (LongRunningLoadSemanticTarget)base.Target;

        public LongRunningLoadSourceEmitter(IncrementalSemanticTarget target)
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
                                   SyntaxFactory.List(createProperties()));
        }

        private IEnumerable<MemberDeclarationSyntax> createProperties()
        {
            // Drawable is the base type which always needs to have the members defined.
            bool isDrawable = Target.FullyQualifiedTypeName == "osu.Framework.Graphics.Drawable";

            yield return SyntaxFactory.PropertyDeclaration(
                                          SyntaxFactory.ParseTypeName("global::System.Type"),
                                          SyntaxFactory.Identifier("KnownType"))
                                      .WithExplicitInterfaceSpecifier(
                                          SyntaxFactory.ExplicitInterfaceSpecifier(
                                              SyntaxFactory.IdentifierName(interface_name)))
                                      .WithExpressionBody(
                                          SyntaxFactory.ArrowExpressionClause(
                                              SyntaxFactory.TypeOfExpression(
                                                  SyntaxFactory.ParseTypeName(Target.GlobalPrefixedTypeName))))
                                      .WithSemicolonToken(
                                          SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            if (Target.IsLongRunning || isDrawable)
            {
                yield return SyntaxFactory.PropertyDeclaration(
                                              SyntaxFactory.PredefinedType(
                                                  SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                                              SyntaxFactory.Identifier("IsLongRunning"))
                                          .WithExplicitInterfaceSpecifier(
                                              SyntaxFactory.ExplicitInterfaceSpecifier(
                                                  SyntaxFactory.IdentifierName(interface_name)))
                                          .WithExpressionBody(
                                              SyntaxFactory.ArrowExpressionClause(
                                                  SyntaxFactory.LiteralExpression(
                                                      Target.IsLongRunning
                                                          ? SyntaxKind.TrueLiteralExpression
                                                          : SyntaxKind.FalseLiteralExpression)))
                                          .WithSemicolonToken(
                                              SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }
        }
    }
}
