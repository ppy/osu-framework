// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace osu.Framework.SourceGeneration.Generators.Dependencies.Data
{
    public readonly struct BackgroundDependencyLoaderAttributeData : IEquatable<BackgroundDependencyLoaderAttributeData>
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

        public bool Equals(BackgroundDependencyLoaderAttributeData other)
        {
            return MethodName == other.MethodName
                   && CanBeNull == other.CanBeNull
                   && Parameters.SequenceEqual(other.Parameters);
        }

        public override bool Equals(object? obj)
        {
            return obj is BackgroundDependencyLoaderAttributeData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = MethodName.GetHashCode();
                hashCode = (hashCode * 397) ^ CanBeNull.GetHashCode();
                hashCode = (hashCode * 397) ^ StructuralComparisons.StructuralEqualityComparer.GetHashCode(Parameters);
                return hashCode;
            }
        }
    }
}
