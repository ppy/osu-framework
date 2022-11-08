// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Emitters
{
    /// <summary>
    /// Emits the statement for a [BackgroundDependencyLoader] attribute.
    /// </summary>
    public class BackgroundDependencyLoaderEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly SyntaxWithSymbol syntax;

        public BackgroundDependencyLoaderEmitter(DependenciesFileEmitter fileEmitter, SyntaxWithSymbol syntax)
        {
            this.fileEmitter = fileEmitter;
            this.syntax = syntax;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            IMethodSymbol methodSymbol = (IMethodSymbol)syntax.Symbol;

            var attributeData = methodSymbol.GetAttributes().Single(a => a.AttributeClass!.Equals(fileEmitter.BackgroundDependencyLoaderAttributeType, SymbolEqualityComparer.Default));

            bool canBeNull = (bool)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "permitNulls").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value
                 ?? false);

            yield return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    createMemberAccessor(),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            methodSymbol.Parameters.Select(p =>
                                SyntaxFactory.Argument(
                                    SyntaxHelpers.GetDependencyInvocation(
                                        fileEmitter.ClassType,
                                        p.Type,
                                        null,
                                        null,
                                        canBeNull || p.Type.NullableAnnotation == NullableAnnotation.Annotated,
                                        false)))))));
        }

        private ExpressionSyntax createMemberAccessor()
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName(fileEmitter.ClassType.ToDisplayString()),
                        SyntaxFactory.IdentifierName(DependenciesFileEmitter.TARGET_PARAMETER_NAME))),
                SyntaxFactory.IdentifierName(syntax.Symbol.Name));
        }
    }
}
