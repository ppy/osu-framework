// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Data;

namespace osu.Framework.SourceGeneration.Emitters
{
    /// <summary>
    /// Emits the statement for a [BackgroundDependencyLoader] attribute.
    /// </summary>
    public class BackgroundDependencyLoaderEmitter : IStatementEmitter
    {
        private readonly DependenciesFileEmitter fileEmitter;
        private readonly BackgroundDependencyLoaderAttributeData data;

        public BackgroundDependencyLoaderEmitter(DependenciesFileEmitter fileEmitter, BackgroundDependencyLoaderAttributeData data)
        {
            this.fileEmitter = fileEmitter;
            this.data = data;
        }

        public IEnumerable<StatementSyntax> Emit()
        {
            yield return SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    createMemberAccessor(),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            data.Parameters.Select(p =>
                                SyntaxFactory.Argument(
                                    SyntaxHelpers.GetDependencyInvocation(
                                        fileEmitter.Candidate.GlobalPrefixedTypeName,
                                        p.GlobalPrefixedTypeName,
                                        null,
                                        null,
                                        p.CanBeNull || data.CanBeNull,
                                        false)))))));
        }

        private ExpressionSyntax createMemberAccessor()
        {
            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.CastExpression(
                        SyntaxFactory.ParseTypeName(fileEmitter.Candidate.GlobalPrefixedTypeName),
                        SyntaxFactory.IdentifierName(DependenciesFileEmitter.TARGET_PARAMETER_NAME))),
                SyntaxFactory.IdentifierName(data.MethodName));
        }
    }
}
