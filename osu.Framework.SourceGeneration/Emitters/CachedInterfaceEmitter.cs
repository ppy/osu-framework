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
    /// Emits the statement for a [Cached] attribute on an implemented interface.
    /// </summary>
    public class CachedInterfaceEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly ITypeSymbol interfaceType;

        public CachedInterfaceEmitter(DependenciesFileEmitter fileEmitter, ITypeSymbol interfaceType)
        {
            this.fileEmitter = fileEmitter;
            this.interfaceType = interfaceType;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            foreach (var attribute in interfaceType.GetAttributes())
            {
                if (!attribute.AttributeClass!.Equals(fileEmitter.CachedAttributeType, SymbolEqualityComparer.Default))
                    continue;

                string cachedType =
                    attribute.NamedArguments.SingleOrDefault(arg => arg.Key == "Type").Value.Value?.ToString()
                    ?? attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString()
                    ?? interfaceType.ToDisplayString();

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
