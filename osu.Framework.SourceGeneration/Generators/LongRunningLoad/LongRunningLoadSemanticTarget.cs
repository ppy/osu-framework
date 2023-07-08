// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.LongRunningLoad
{
    public class LongRunningLoadSemanticTarget : IncrementalSemanticTarget
    {
        public bool IsLongRunning { get; private set; }

        public LongRunningLoadSemanticTarget(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            : base(classSyntax, semanticModel)
        {
        }

        protected override bool CheckValid(INamedTypeSymbol symbol)
        {
            INamedTypeSymbol? s = symbol;

            while (s != null)
            {
                if (isDrawableType(s))
                    return true;

                s = s.BaseType;
            }

            return false;
        }

        // This source generator never overrides.
        protected override bool CheckNeedsOverride(INamedTypeSymbol symbol) => false;

        protected override void Process(INamedTypeSymbol symbol)
        {
            if (FullyQualifiedTypeName == "osu.Framework.Graphics.Drawable")
                return;

            IsLongRunning = symbol.GetAttributes().Any(SyntaxHelpers.IsLongRunningLoadAttribute);
        }

        private static bool isDrawableType(INamedTypeSymbol type)
            => SyntaxHelpers.GetFullyQualifiedTypeName(type) == "osu.Framework.Graphics.Drawable";
    }
}
