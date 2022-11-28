// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration
{
    public class GeneratorClassCandidate : IEquatable<GeneratorClassCandidate>
    {
        public readonly ClassDeclarationSyntax ClassSyntax;
        public readonly ITypeSymbol Symbol;
        public readonly string TypeName;
        public readonly HashSet<SyntaxWithSymbol> ResolvedMembers = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<SyntaxWithSymbol> CachedMembers = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<SyntaxWithSymbol> CachedClasses = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<SyntaxWithSymbol> DependencyLoaderMemebers = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<ITypeSymbol> CachedInterfaces = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        public GeneratorClassCandidate(ClassDeclarationSyntax classSyntax, ITypeSymbol symbol)
        {
            ClassSyntax = classSyntax;
            Symbol = symbol;
            TypeName = Symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        }

        public bool Equals(GeneratorClassCandidate? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(TypeName, other.TypeName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((GeneratorClassCandidate)obj);
        }

        public override int GetHashCode() => TypeName.GetHashCode();
    }
}
