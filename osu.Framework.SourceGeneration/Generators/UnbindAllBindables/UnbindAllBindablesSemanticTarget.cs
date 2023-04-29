// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.UnbindAllBindables
{
    public class UnbindAllBindablesSemanticTarget : IncrementalSemanticTarget
    {
        public readonly List<string> BindableFieldNames = new List<string>();

        public UnbindAllBindablesSemanticTarget(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            : base(classSyntax, semanticModel)
        {
        }

        protected override bool CheckValid(INamedTypeSymbol symbol)
        {
            if (!isDrawableSubType(symbol))
                return false;

            return isDrawableType(symbol)
                   || symbol.GetMembers().OfType<IFieldSymbol>().Any(m => m.Type.AllInterfaces.Any(SyntaxHelpers.IsIUnbindableInterface));
        }

        protected override bool CheckNeedsOverride(INamedTypeSymbol symbol) => !isDrawableType(symbol);

        protected override void Process(INamedTypeSymbol symbol)
        {
            BindableFieldNames.AddRange(symbol.GetMembers().OfType<IFieldSymbol>()
                                              .Where(m => m.Type.AllInterfaces.Any(SyntaxHelpers.IsIUnbindableInterface))
                                              .Select(m => m.Name));
        }

        private bool isDrawableSubType(INamedTypeSymbol type)
        {
            INamedTypeSymbol? s = type;

            while (s != null)
            {
                if (isDrawableType(s))
                    return true;

                s = s.BaseType;
            }

            return false;
        }

        private bool isDrawableType(INamedTypeSymbol type)
            => type.Name == "Drawable" && SyntaxHelpers.GetFullyQualifiedTypeName(type) == "osu.Framework.Graphics.Drawable";
    }
}
