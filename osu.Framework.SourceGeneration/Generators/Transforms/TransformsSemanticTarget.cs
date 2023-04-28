// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.Transforms
{
    public class TransformsSemanticTarget : IncrementalSemanticTarget
    {
        public readonly List<TransformMemberData> Members = new List<TransformMemberData>();

        public TransformsSemanticTarget(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            : base(classSyntax, semanticModel)
        {
        }

        protected override bool CheckValid(INamedTypeSymbol symbol) => true;

        protected override bool CheckNeedsOverride(INamedTypeSymbol symbol) => false;

        protected override void Process(INamedTypeSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
            {
                if (member is not IFieldSymbol and not IPropertySymbol)
                    continue;

                AttributeData? attribute = member.GetAttributes().FirstOrDefault(attrib => attrib.AttributeClass?.Name == "TransformGeneratorAttribute");
                if (attribute == null)
                    continue;

                string type = member switch
                {
                    IFieldSymbol field => SyntaxHelpers.GetGlobalPrefixedTypeName(field.Type)!,
                    IPropertySymbol property => SyntaxHelpers.GetGlobalPrefixedTypeName(property.Type)!,
                    _ => throw new Exception("Should not be reached")
                };

                string? name = (string?)attribute.ConstructorArguments.FirstOrDefault().Value;
                name ??= member.Name;

                Members.Add(new TransformMemberData(
                    name,
                    member.Name,
                    type));
            }
        }
    }
}
