// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Data
{
    public readonly struct BackgroundDependencyLoaderAttributeData
    {
        public readonly string MethodName;
        public readonly bool CanBeNull;
        public readonly ImmutableArray<BackgroundDependencyLoaderParameterData> Parameters;

        public BackgroundDependencyLoaderAttributeData(string methodName, bool canBeNull, ImmutableArray<BackgroundDependencyLoaderParameterData> parameters)
        {
            MethodName = methodName;
            CanBeNull = canBeNull;
            Parameters = parameters;
        }

        public static BackgroundDependencyLoaderAttributeData FromMethod(IMethodSymbol method, AttributeData attributeData)
        {
            bool canBeNull = (bool)
                (attributeData.NamedArguments.SingleOrDefault(arg => arg.Key == "permitNulls").Value.Value
                 ?? attributeData.ConstructorArguments.ElementAtOrDefault(0).Value
                 ?? false);

            ImmutableArray<BackgroundDependencyLoaderParameterData>.Builder parameterBuilder = ImmutableArray.CreateBuilder<BackgroundDependencyLoaderParameterData>(method.Parameters.Length);

            foreach (var parameter in method.Parameters)
                parameterBuilder.Add(BackgroundDependencyLoaderParameterData.FromParameter(parameter));

            return new BackgroundDependencyLoaderAttributeData(method.Name, canBeNull, parameterBuilder.MoveToImmutable());
        }
    }
}
