// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct ResolvedAttributeData
    {
        public readonly string Type;
        public readonly string PropertyName;

        public readonly string? ParentType;
        public readonly string? CachedName;
        public readonly bool CanBeNull;

        public ResolvedAttributeData(string type, string propertyName, string? parentType, string? cachedName, bool canBeNull)
        {
            Type = type;
            PropertyName = propertyName;

            ParentType = parentType;
            CachedName = cachedName;
            CanBeNull = canBeNull;

            // When a parent type exists, infer the property name if one is not provided
            if (parentType != null)
                CachedName ??= propertyName;
        }

        public static ResolvedAttributeData FromProperty(IPropertySymbol symbol, AttributeData attributeData)
        {
            string? parentType =
                attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Parent").Value.Value?.ToString()
                ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();

            string? name = (string?)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(1).Value);

            bool canBeNull = (bool)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "CanBeNull").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(2).Value
                 ?? false);

            canBeNull |= symbol.NullableAnnotation == NullableAnnotation.Annotated;

            return new ResolvedAttributeData(symbol.Type.ToDisplayString(), symbol.Name, parentType, name, canBeNull);
        }
    }
}
