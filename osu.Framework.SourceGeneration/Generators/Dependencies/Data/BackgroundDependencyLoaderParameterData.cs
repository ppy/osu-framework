// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Generators.Dependencies.Data
{
    public readonly struct BackgroundDependencyLoaderParameterData : IEquatable<BackgroundDependencyLoaderParameterData>
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

        public bool Equals(BackgroundDependencyLoaderParameterData other)
        {
            return GlobalPrefixedTypeName == other.GlobalPrefixedTypeName
                   && CanBeNull == other.CanBeNull;
        }

        public override bool Equals(object? obj)
        {
            return obj is BackgroundDependencyLoaderParameterData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (GlobalPrefixedTypeName.GetHashCode() * 397) ^ CanBeNull.GetHashCode();
            }
        }
    }
}
