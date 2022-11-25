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
    /// Emits the statement for a [Resolved] attribute.
    /// </summary>
    public class ResolvedMemberEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly SyntaxWithSymbol syntax;

        public ResolvedMemberEmitter(DependenciesFileEmitter fileEmitter, SyntaxWithSymbol syntax)
        {
            this.fileEmitter = fileEmitter;
            this.syntax = syntax;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            IPropertySymbol propertySymbol = (IPropertySymbol)syntax.Symbol;
            ITypeSymbol propertyType = propertySymbol.Type;

            foreach (var attribute in syntax.Symbol.GetAttributes())
            {
                if (!attribute.AttributeClass!.Equals(fileEmitter.ResolvedAttributeType, SymbolEqualityComparer.Default))
                    continue;

                string? resolvedParentType =
                    attribute.NamedArguments.SingleOrDefault(arg => arg.Key == "Parent").Value.Value?.ToString()
                    ?? attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();

                string? resolvedName = (string?)
                    (attribute.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                     ?? attribute.ConstructorArguments.ElementAtOrDefault(1).Value);

                // When a parent type exists, infer the property name if one is not provided
                if (resolvedParentType != null)
                    resolvedName ??= syntax.Symbol.Name;

                bool resolvedCanBeNull = (bool)
                    (attribute.NamedArguments.SingleOrDefault(arg => arg.Key == "CanBeNull").Value.Value
                     ?? attribute.ConstructorArguments.ElementAtOrDefault(2).Value
                     ?? false);

                resolvedCanBeNull |= propertySymbol.NullableAnnotation == NullableAnnotation.Annotated;

                yield return SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        createMemberAccessor(),
                        SyntaxHelpers.GetDependencyInvocation(
                            fileEmitter.ClassType,
                            propertyType,
                            resolvedName,
                            resolvedParentType,
                            resolvedCanBeNull,
                            true)));
            }
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
