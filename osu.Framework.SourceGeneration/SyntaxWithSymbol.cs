// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration
{
    public class SyntaxWithSymbol : IEquatable<SyntaxWithSymbol>
    {
        public readonly MemberDeclarationSyntax Syntax;
        public readonly ISymbol Symbol;

        public SyntaxWithSymbol(GeneratorSyntaxContext context, MemberDeclarationSyntax syntax)
        {
            Syntax = syntax;

            if (syntax is FieldDeclarationSyntax field)
                Symbol = context.SemanticModel.GetDeclaredSymbol(field.Declaration.Variables.Single())!;
            else
                Symbol = context.SemanticModel.GetDeclaredSymbol(syntax)!;
        }

        public bool Equals(SyntaxWithSymbol? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Syntax == other.Syntax;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((SyntaxWithSymbol)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Syntax.GetHashCode() * 397;
            }
        }
    }
}
