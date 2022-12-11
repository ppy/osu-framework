// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct BackgroundDependencyLoaderParameterData
    {
        public readonly string GlobalPrefixedTypeName;
        public readonly bool CanBeNull;

        public BackgroundDependencyLoaderParameterData(string globalPrefixedTypeName, bool canBeNull)
        {
            GlobalPrefixedTypeName = globalPrefixedTypeName;
            CanBeNull = canBeNull;
        }

        public static BackgroundDependencyLoaderParameterData FromParameter(IParameterSymbol parameter)
        {
            string globalPrefixedTypeName = SyntaxHelpers.GetGlobalPrefixedTypeName(parameter.Type)!;
            bool canBeNull = parameter.NullableAnnotation == NullableAnnotation.Annotated;

            return new BackgroundDependencyLoaderParameterData(globalPrefixedTypeName, canBeNull);
        }
    }
}
