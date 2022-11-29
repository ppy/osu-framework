// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct CachedAttributeData : IEquatable<CachedAttributeData>
    {
        public readonly string? Type;
        public readonly string? Name;
        public readonly string? PropertyName;

        public bool Equals(CachedAttributeData other)
        {
            return Type == other.Type && Name == other.Name && PropertyName == other.PropertyName;
        }

        public override bool Equals(object? obj)
        {
            return obj is CachedAttributeData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PropertyName != null ? PropertyName.GetHashCode() : 0);
                return hashCode;
            }
        }

        public CachedAttributeData(string? type, string? name, string? propertyName)
        {
            Type = type;
            Name = name;
            PropertyName = propertyName;
        }

        public static CachedAttributeData FromPropertyOrField(ISymbol symbol, AttributeData attributeData)
        {
            if (symbol is not IPropertySymbol && symbol is not IFieldSymbol)
                throw new InvalidOperationException("Cannot created cached attribute from a non-property/field symbol.");

            string? type =
                attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Type").Value.Value?.ToString()
                ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();

            string? name = (string?)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(1).Value);

            return new CachedAttributeData(type, name, symbol.Name);
        }

        public static CachedAttributeData FromInterfaceOrClass(ITypeSymbol typeSymbol, AttributeData attributeData)
        {
            string type =
                attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Type").Value.Value?.ToString()
                ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString()
                ?? typeSymbol.ToDisplayString();

            string? name = (string?)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(1).Value);

            return new CachedAttributeData(type, name, null);
        }
    }
}
