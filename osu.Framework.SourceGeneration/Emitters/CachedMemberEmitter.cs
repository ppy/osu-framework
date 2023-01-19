// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Data;

namespace osu.Framework.SourceGeneration.Emitters
{
    /// <summary>
    /// Emits the statement for a [Cached] attribute on a member of the class.
    /// </summary>
    public class CachedMemberEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly CachedAttributeData data;

        public CachedMemberEmitter(DependenciesFileEmitter fileEmitter, CachedAttributeData data)
        {
            this.fileEmitter = fileEmitter;
            this.data = data;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            yield return SyntaxFactory.ExpressionStatement(
                SyntaxHelpers.CacheDependencyInvocation(
                    fileEmitter.Candidate.GlobalPrefixedTypeName,
                    createMemberAccessor(),
                    data.GlobalPrefixedTypeName,
                    data.Name,
                    data.PropertyName
                ));
        }

        private ExpressionSyntax createMemberAccessor()
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName(fileEmitter.Candidate.GlobalPrefixedTypeName),
                        SyntaxFactory.IdentifierName(DependenciesFileEmitter.TARGET_PARAMETER_NAME))),
                SyntaxFactory.IdentifierName(data.PropertyName!));
        }
    }
}
