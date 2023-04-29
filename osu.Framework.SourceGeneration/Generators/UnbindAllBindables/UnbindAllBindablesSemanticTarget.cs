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
        public readonly List<BindableDefinition> Bindables = new List<BindableDefinition>();

        public UnbindAllBindablesSemanticTarget(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            : base(classSyntax, semanticModel)
        {
        }

        protected override bool CheckValid(INamedTypeSymbol symbol)
        {
            if (!isDrawableSubType(symbol))
                return false;

            return isDrawableType(symbol) || enumerateBindables(symbol).Any();
        }

        protected override bool CheckNeedsOverride(INamedTypeSymbol symbol) => !isDrawableType(symbol);

        protected override void Process(INamedTypeSymbol symbol)
        {
            foreach (IFieldSymbol bindableField in enumerateBindables(symbol))
            {
                if (bindableField.IsImplicitlyDeclared)
                {
                    // Compiler generated names are of the form "<Name>k__BackingField".
                    string name = bindableField.Name.Substring(1, bindableField.Name.IndexOf('>') - 1);
                    string containingType = GlobalPrefixedTypeName;

                    // In the case of explicit auto-property implementations, the name will contain the containing type (interface name).
                    int containingTypeIndex = name.LastIndexOf('.');

                    if (containingTypeIndex != -1)
                    {
                        containingType = name.Substring(0, containingTypeIndex);
                        name = name.Substring(containingTypeIndex + 1);
                    }

                    Bindables.Add(new BindableDefinition(name, containingType));
                }
                else
                    Bindables.Add(new BindableDefinition(bindableField.Name, GlobalPrefixedTypeName));
            }
        }

        private IEnumerable<IFieldSymbol> enumerateBindables(INamedTypeSymbol symbol)
        {
            return symbol.GetMembers()
                         .OfType<IFieldSymbol>()
                         .Where(m => !m.IsStatic)
                         .Where(m => m.Type.AllInterfaces.Any(SyntaxHelpers.IsIUnbindableInterface));
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
