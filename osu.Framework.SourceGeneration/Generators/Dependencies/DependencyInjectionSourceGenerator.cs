// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Generators.Dependencies.Emitters;

namespace osu.Framework.SourceGeneration.Generators.Dependencies
{
    [Generator]
    public class DependencyInjectionSourceGenerator : AbstractIncrementalGenerator
    {
        protected override IncrementalSemanticTarget CreateSemanticTarget(ClassDeclarationSyntax node, SemanticModel semanticModel)
            => new DependenciesClassCandidate(node, semanticModel);

        protected override IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target)
            => new DependenciesFileEmitter((DependenciesClassCandidate)target);
    }
}
