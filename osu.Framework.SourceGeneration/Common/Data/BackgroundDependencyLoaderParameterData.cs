// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct BackgroundDependencyLoaderParameterData
    {
        public readonly string Type;
        public readonly bool CanBeNull;

        public BackgroundDependencyLoaderParameterData(string type, bool canBeNull)
        {
            Type = type;
            CanBeNull = canBeNull;
        }

        public static BackgroundDependencyLoaderParameterData FromParameter(IParameterSymbol parameter)
        {
            string type = parameter.Type.ToDisplayString();
            bool canBeNull = parameter.NullableAnnotation == NullableAnnotation.Annotated;

            return new BackgroundDependencyLoaderParameterData(type, canBeNull);
        }
    }
}
