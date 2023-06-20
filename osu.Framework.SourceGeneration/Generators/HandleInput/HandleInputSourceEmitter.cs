// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.HandleInput
{
    public class HandleInputSourceEmitter : IncrementalSourceEmitter
    {
        private const string interface_name = "global::osu.Framework.Input.ISourceGeneratedHandleInputCache";

        protected override string FileSuffix => "HandleInput";

        public new HandleInputSemanticTarget Target => (HandleInputSemanticTarget)base.Target;

        public HandleInputSourceEmitter(IncrementalSemanticTarget target)
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

            if (Target.RequestsPositionalInput || isDrawable)
                yield return createInputMember("RequestsPositionalInput", Target.RequestsPositionalInput);

            if (Target.RequestsNonPositionalInput || isDrawable)
                yield return createInputMember("RequestsNonPositionalInput", Target.RequestsNonPositionalInput);

            MemberDeclarationSyntax createInputMember(string name, bool value) =>
                SyntaxFactory.PropertyDeclaration(
                                 SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)),
                                 SyntaxFactory.Identifier(name))
                             .WithExplicitInterfaceSpecifier(
                                 SyntaxFactory.ExplicitInterfaceSpecifier(
                                     SyntaxFactory.IdentifierName(interface_name)))
                             .WithExpressionBody(
                                 SyntaxFactory.ArrowExpressionClause(
                                     SyntaxFactory.LiteralExpression(
                                         value
                                             ? SyntaxKind.TrueLiteralExpression
                                             : SyntaxKind.FalseLiteralExpression)))
                             .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }
    }
}
