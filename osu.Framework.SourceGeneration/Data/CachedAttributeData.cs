// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct CachedAttributeData : IEquatable<CachedAttributeData>
    {
        public readonly string? GlobalPrefixedTypeName;
        public readonly string? Name;
        public readonly string? PropertyName;

        public bool Equals(CachedAttributeData other)
        {
            return GlobalPrefixedTypeName == other.GlobalPrefixedTypeName && Name == other.Name && PropertyName == other.PropertyName;
        }

        public override bool Equals(object? obj)
        {
            return obj is CachedAttributeData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (GlobalPrefixedTypeName != null ? GlobalPrefixedTypeName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PropertyName != null ? PropertyName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public CachedAttributeData(string? globalPrefixedTypeName, string? name, string? propertyName)
        {
            GlobalPrefixedTypeName = globalPrefixedTypeName;
            Name = name;
            PropertyName = propertyName;
        }

        public static CachedAttributeData FromPropertyOrField(ISymbol symbol, AttributeData attributeData)
        {
            if (symbol is not IPropertySymbol && symbol is not IFieldSymbol)
                throw new InvalidOperationException("Cannot created cached attribute from a non-property/field symbol.");

            object? typeSymbolCandidate =
                attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Type").Value.Value
                ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value;
            string? globalPrefixedTypeName = SyntaxHelpers.GetGlobalPrefixedTypeName(typeSymbolCandidate as ITypeSymbol);

            string? name = (string?)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(1).Value);

            return new CachedAttributeData(globalPrefixedTypeName, name, symbol.Name);
        }

        public static CachedAttributeData FromInterfaceOrClass(ITypeSymbol typeSymbol, AttributeData attributeData)
        {
            object typeSymbolCandidate =
                attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Type").Value.Value
                ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value
                ?? typeSymbol;

            string? globalPrefixedTypeName = SyntaxHelpers.GetGlobalPrefixedTypeName(typeSymbolCandidate as ITypeSymbol);

            string? name = (string?)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(1).Value);

            return new CachedAttributeData(globalPrefixedTypeName, name, null);
        }
    }
}
