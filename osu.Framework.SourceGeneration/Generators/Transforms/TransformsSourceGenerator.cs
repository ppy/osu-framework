// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.Transforms
{
    [Generator]
    public class TransformsSourceGenerator : AbstractIncrementalGenerator
    {
        protected override IncrementalSemanticTarget CreateSemanticTarget(ClassDeclarationSyntax node, SemanticModel semanticModel)
            => new TransformsSemanticTarget(node, semanticModel);

        protected override IncrementalSourceEmitter CreateSourceEmitter(IncrementalSemanticTarget target)
            => new TransformsSourceEmitter(target);

        protected override bool IsSyntaxTarget(ClassDeclarationSyntax syntaxNode)
        {
            return syntaxNode.Members
                             .SelectMany(member => member.AttributeLists)
                             .SelectMany(list => list.Attributes)
                             .Any(attrib => attrib.Name.ToString().Contains("TransformGenerator"));
        }
    }
}
