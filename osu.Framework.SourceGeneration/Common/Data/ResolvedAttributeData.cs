// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct ResolvedAttributeData
    {
        public readonly string GlobalPrefixedTypeName;
        public readonly string PropertyName;

        public readonly string? GlobalPrefixedParentTypeName;
        public readonly string? CachedName;
        public readonly bool CanBeNull;

        public ResolvedAttributeData(string globalPrefixedTypeName, string propertyName, string? globalPrefixedParentTypeName, string? cachedName, bool canBeNull)
        {
            GlobalPrefixedTypeName = globalPrefixedTypeName;
            PropertyName = propertyName;

            GlobalPrefixedParentTypeName = globalPrefixedParentTypeName;

            CachedName = cachedName;
            CanBeNull = canBeNull;

            // When a parent type exists, infer the property name if one is not provided
            if (globalPrefixedParentTypeName != null)
                CachedName ??= propertyName;
        }

        public static ResolvedAttributeData FromProperty(IPropertySymbol symbol, AttributeData attributeData)
        {
            object? parentTypeCandidate =
                attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Parent").Value.Value
                ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value;

            string? globalPrefixedParentTypeName = SyntaxHelpers.GetGlobalPrefixedTypeName(parentTypeCandidate as ITypeSymbol);

            string? name = (string?)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "Name").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(1).Value);

            bool canBeNull = (bool)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "CanBeNull").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(2).Value
                 ?? false);

            // This allows determining whether null is permitted based on the NRT annotation of the property.
            // However, this doesn't work on mobile projects due to using Xamarin, therefore it cannot be relied on and is temporarily disabled.
            // todo: enable this once all mobile projects target .NET 6
            // canBeNull |= symbol.NullableAnnotation == NullableAnnotation.Annotated;

            return new ResolvedAttributeData(SyntaxHelpers.GetGlobalPrefixedTypeName(symbol.Type)!, symbol.Name, globalPrefixedParentTypeName, name, canBeNull);
        }
    }
}
