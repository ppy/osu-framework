// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.Transforms
{
    public class TransformsSourceEmitter : IncrementalSourceEmitter
    {
        public new TransformsSemanticTarget Target => (TransformsSemanticTarget)base.Target;

        public TransformsSourceEmitter(IncrementalSemanticTarget target)
            : base(target)
        {
        }

        protected override string FileSuffix => "Transforms";

        protected override ClassDeclarationSyntax ConstructClass(ClassDeclarationSyntax initialClass)
        {
            return initialClass
                .WithMembers(
                    SyntaxFactory.List(Target.Members.Select(createMember)));
        }

        private MemberDeclarationSyntax createMember(TransformMemberData data)
        {
            return SyntaxFactory.MethodDeclaration(
                                    SyntaxFactory.GenericName(
                                                     SyntaxFactory.Identifier("global::osu.Framework.Graphics.Transforms.Transform"))
                                                 .WithTypeArgumentList(
                                                     SyntaxFactory.TypeArgumentList(
                                                         SyntaxFactory.SeparatedList(
                                                             new[]
                                                             {
                                                                 SyntaxFactory.ParseTypeName(data.GlobalPrefixedTypeName),
                                                                 SyntaxFactory.IdentifierName("TEasing"),
                                                                 SyntaxFactory.ParseTypeName(Target.GlobalPrefixedTypeName)
                                                             }))),
                                    SyntaxFactory.Identifier($"Create{data.MethodName}Transform"))
                                .WithModifiers(
                                    SyntaxFactory.TokenList(
                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                                .WithTypeParameterList(
                                    SyntaxFactory.TypeParameterList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.TypeParameter(
                                                SyntaxFactory.Identifier("TEasing")))))
                                .WithParameterList(
                                    SyntaxFactory.ParameterList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Parameter(
                                                             SyntaxFactory.Identifier("grouping"))
                                                         .WithType(
                                                             SyntaxFactory.NullableType(
                                                                 SyntaxFactory.PredefinedType(
                                                                     SyntaxFactory.Token(SyntaxKind.StringKeyword))))
                                                         .WithDefault(
                                                             SyntaxFactory.EqualsValueClause(
                                                                 SyntaxFactory.LiteralExpression(
                                                                     SyntaxKind.NullLiteralExpression))))))
                                .WithConstraintClauses(
                                    SyntaxFactory.SingletonList(
                                        SyntaxFactory.TypeParameterConstraintClause(
                                                         SyntaxFactory.IdentifierName("TEasing"))
                                                     .WithConstraints(
                                                         SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(
                                                             SyntaxFactory.TypeConstraint(
                                                                 SyntaxFactory.IdentifierName("global::osu.Framework.Graphics.Transforms.IEasingFunction"))))))
                                .WithBody(
                                    SyntaxFactory.Block(
                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                            SyntaxFactory.ReturnStatement(
                                                SyntaxFactory.ObjectCreationExpression(
                                                                 SyntaxFactory.GenericName(
                                                                                  SyntaxFactory.Identifier("global::osu.Framework.Graphics.Transforms.TransformCustom"))
                                                                              .WithTypeArgumentList(
                                                                                  SyntaxFactory.TypeArgumentList(
                                                                                      SyntaxFactory.SeparatedList(
                                                                                          new[]
                                                                                          {
                                                                                              SyntaxFactory.ParseTypeName(data.GlobalPrefixedTypeName),
                                                                                              SyntaxFactory.IdentifierName("TEasing"),
                                                                                              SyntaxFactory.ParseTypeName(Target.GlobalPrefixedTypeName)
                                                                                          }))))
                                                             .WithArgumentList(
                                                                 SyntaxFactory.ArgumentList(
                                                                     SyntaxFactory.SeparatedList(
                                                                         new[]
                                                                         {
                                                                             SyntaxFactory.Argument(
                                                                                 SyntaxFactory.InvocationExpression(
                                                                                                  SyntaxFactory.IdentifierName(
                                                                                                      SyntaxFactory.Identifier(
                                                                                                          SyntaxFactory.TriviaList(),
                                                                                                          SyntaxKind.NameOfKeyword,
                                                                                                          "nameof",
                                                                                                          "nameof",
                                                                                                          SyntaxFactory.TriviaList())))
                                                                                              .WithArgumentList(
                                                                                                  SyntaxFactory.ArgumentList(
                                                                                                      SyntaxFactory.SingletonSeparatedList(
                                                                                                          SyntaxFactory.Argument(
                                                                                                              SyntaxFactory.IdentifierName(data.PropertyOrFieldName)))))),
                                                                             SyntaxFactory.Argument(
                                                                                 SyntaxFactory.SimpleLambdaExpression(
                                                                                                  SyntaxFactory.Parameter(
                                                                                                      SyntaxFactory.Identifier("d")))
                                                                                              .WithExpressionBody(
                                                                                                  SyntaxFactory.MemberAccessExpression(
                                                                                                      SyntaxKind.SimpleMemberAccessExpression,
                                                                                                      SyntaxFactory.IdentifierName("d"),
                                                                                                      SyntaxFactory.IdentifierName(data.PropertyOrFieldName)))),
                                                                             SyntaxFactory.Argument(
                                                                                 SyntaxFactory.ParenthesizedLambdaExpression()
                                                                                              .WithParameterList(
                                                                                                  SyntaxFactory.ParameterList(
                                                                                                      SyntaxFactory.SeparatedList(
                                                                                                          new[]
                                                                                                          {
                                                                                                              SyntaxFactory.Parameter(
                                                                                                                  SyntaxFactory.Identifier("d")),
                                                                                                              SyntaxFactory.Parameter(
                                                                                                                  SyntaxFactory.Identifier("value"))
                                                                                                          })))
                                                                                              .WithExpressionBody(
                                                                                                  SyntaxFactory.AssignmentExpression(
                                                                                                      SyntaxKind.SimpleAssignmentExpression,
                                                                                                      SyntaxFactory.MemberAccessExpression(
                                                                                                          SyntaxKind.SimpleMemberAccessExpression,
                                                                                                          SyntaxFactory.IdentifierName("d"),
                                                                                                          SyntaxFactory.IdentifierName(data.PropertyOrFieldName)),
                                                                                                      SyntaxFactory.IdentifierName("value")))),
                                                                             SyntaxFactory.Argument(
                                                                                 SyntaxFactory.IdentifierName("grouping"))
                                                                         })))))));
        }
    }
}
