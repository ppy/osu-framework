// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Generators.Dependencies.Data;

namespace osu.Framework.SourceGeneration.Generators.Dependencies
{
    public class DependenciesClassCandidate : IncrementalSemanticTarget
    {
        public readonly HashSet<CachedAttributeData> CachedInterfaces = new HashSet<CachedAttributeData>();
        public readonly HashSet<CachedAttributeData> CachedMembers = new HashSet<CachedAttributeData>();
        public readonly HashSet<CachedAttributeData> CachedClasses = new HashSet<CachedAttributeData>();
        public readonly HashSet<ResolvedAttributeData> ResolvedMembers = new HashSet<ResolvedAttributeData>();
        public readonly HashSet<BackgroundDependencyLoaderAttributeData> DependencyLoaderMembers = new HashSet<BackgroundDependencyLoaderAttributeData>();

        public DependenciesClassCandidate(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            : base(classSyntax, semanticModel)
        {
        }

        protected override bool CheckValid(INamedTypeSymbol symbol) => symbol.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface);

        protected override bool CheckNeedsOverride(INamedTypeSymbol symbol) => symbol.BaseType!.AllInterfaces.Any(SyntaxHelpers.IsIDependencyInjectionCandidateInterface);

        protected override void Process(INamedTypeSymbol symbol)
        {
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
    }
}
