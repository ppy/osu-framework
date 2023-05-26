// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.HandleInput
{
    [Generator]
    public class HandleInputSourceGenerator : AbstractIncrementalGenerator
    {
        protected override IncrementalSemanticTarget CreateSemanticTarget(ClassDeclarationSyntax node, SemanticModel semanticModel)
            => new HandleInputSemanticTarget(node, semanticModel);

        protected override IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target)
            => new HandleInputSourceEmitter(target);
    }
}
