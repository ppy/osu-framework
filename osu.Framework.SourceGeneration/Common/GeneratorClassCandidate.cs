// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using osu.Framework.SourceGeneration.Data;

namespace osu.Framework.SourceGeneration
{
    public class GeneratorClassCandidate : IEquatable<GeneratorClassCandidate>
    {
        public readonly string TypeName;
        public readonly string FullyQualifiedTypeName;
        public readonly bool NeedsOverride;
        public readonly string? ContainingNamespace;

        public readonly List<string> TypeHierarchy = new List<string>();

        public readonly HashSet<CachedAttributeData> CachedInterfaces = new HashSet<CachedAttributeData>();
        public readonly HashSet<CachedAttributeData> CachedMembers = new HashSet<CachedAttributeData>();
        public readonly HashSet<CachedAttributeData> CachedClasses = new HashSet<CachedAttributeData>();
        public readonly HashSet<ResolvedAttributeData> ResolvedMembers = new HashSet<ResolvedAttributeData>();
        public readonly HashSet<BackgroundDependencyLoaderAttributeData> DependencyLoaderMembers = new HashSet<BackgroundDependencyLoaderAttributeData>();

        public GeneratorClassCandidate(INamedTypeSymbol symbol)
        {
            TypeName = symbol.ToDisplayString();
            FullyQualifiedTypeName = SyntaxHelpers.GetFullyQualifiedTypeName(symbol);
            NeedsOverride = symbol.BaseType != null && symbol.BaseType.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface);
            ContainingNamespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();

            ITypeSymbol? containingType = symbol;

            while (containingType != null)
            {
                TypeHierarchy.Add(createTypeName(containingType));
                containingType = containingType.ContainingType ?? null;
            }

            // Process any [Cached] attributes on any interface on the class excluding base types.
            foreach (var iFace in SyntaxHelpers.GetDeclaredInterfacesOnType(symbol))
            {
                // Add an entry if this interface has a cached attribute.
                foreach (var attrib in iFace.GetAttributes().Where(SyntaxHelpers.IsCachedAttribute))
                    CachedInterfaces.Add(CachedAttributeData.FromInterfaceOrClass(iFace, attrib));
            }

            // Process any [Cached] attributes on the class.
            foreach (var attrib in symbol.GetAttributes().Where(SyntaxHelpers.IsCachedAttribute))
                CachedClasses.Add(CachedAttributeData.FromInterfaceOrClass(symbol, attrib));

            // Process any attributes of members of the class.
            foreach (var member in symbol.GetMembers())
            {
                switch (member)
                {
                    case IFieldSymbol field:
                    {
                        foreach (var attrib in field.GetAttributes().Where(SyntaxHelpers.IsCachedAttribute))
                            CachedMembers.Add(CachedAttributeData.FromPropertyOrField(field, attrib));

                        break;
                    }

                    case IPropertySymbol property:
                    {
                        foreach (var attrib in property.GetAttributes())
                        {
                            if (SyntaxHelpers.IsCachedAttribute(attrib))
                                CachedMembers.Add(CachedAttributeData.FromPropertyOrField(property, attrib));
                            if (SyntaxHelpers.IsResolvedAttribute(attrib))
                                ResolvedMembers.Add(ResolvedAttributeData.FromProperty(property, attrib));
                        }

                        break;
                    }

                    case IMethodSymbol method:
                    {
                        foreach (var attrib in method.GetAttributes().Where(SyntaxHelpers.IsBackgroundDependencyLoaderAttribute))
                            DependencyLoaderMembers.Add(BackgroundDependencyLoaderAttributeData.FromMethod(method, attrib));

                        break;
                    }
                }
            }
        }

        private static string createTypeName(ITypeSymbol typeSymbol)
        {
            string name = typeSymbol.Name;

            if (typeSymbol is INamedTypeSymbol named && named.TypeParameters.Length > 0)
                name += $@"<{string.Join(@", ", named.TypeParameters)}>";

            return name;
        }

        public bool Equals(GeneratorClassCandidate? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return FullyQualifiedTypeName == other.FullyQualifiedTypeName;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((GeneratorClassCandidate)obj);
        }

        public override int GetHashCode()
        {
            return FullyQualifiedTypeName.GetHashCode();
        }
    }
}
