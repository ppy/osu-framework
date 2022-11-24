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
    /// Emits the statement for a [Cached] attribute on the class itself.
    /// </summary>
    public class CachedClassEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly SyntaxWithSymbol syntax;

        public CachedClassEmitter(DependenciesFileEmitter fileEmitter, SyntaxWithSymbol syntax)
        {
            this.fileEmitter = fileEmitter;
            this.syntax = syntax;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            foreach (var attribute in syntax.Symbol.GetAttributes())
            {
                if (!attribute.AttributeClass!.Equals(fileEmitter.CachedAttributeType, SymbolEqualityComparer.Default))
                    continue;

                string cachedType =
                    attribute.NamedArguments.SingleOrDefault(arg => arg.Key == "Type").Value.Value?.ToString()
                    ?? attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString()
                    ?? fileEmitter.ClassType.ToDisplayString();

                string? cachedName = (string?)
                    (attribute.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                     ?? attribute.ConstructorArguments.ElementAtOrDefault(1).Value);

                yield return SyntaxFactory.ExpressionStatement(
                    SyntaxHelpers.CacheDependencyInvocation(
                        fileEmitter.ClassType,
                        SyntaxFactory.IdentifierName(DependenciesFileEmitter.TARGET_PARAMETER_NAME),
                        cachedType,
                        cachedName,
                        null
                    ));
            }
        }
    }
}
