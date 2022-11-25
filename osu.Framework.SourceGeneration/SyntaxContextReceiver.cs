// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration
{
    public class SyntaxContextReceiver : ISyntaxContextReceiver
    {
        public const string IDRAWABLE_INTERFACE_NAME = "osu.Framework.Graphics.IDrawable";

        public readonly Dictionary<ISymbol, GeneratorClassCandidate> CandidateClasses = new Dictionary<ISymbol, GeneratorClassCandidate>(SymbolEqualityComparer.Default);

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            SyntaxNode syntaxNode = context.Node;

            if (syntaxNode is not ClassDeclarationSyntax classSyntax)
                return;

            if (!classSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                return;

            INamedTypeSymbol? symbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);

            if (symbol == null)
                return;

            if (classSyntax.Ancestors().OfType<ClassDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return;

            // Determine if the class is a candidate for the source generator.
            // Classes may be candidates even if they don't resolve/cache anything themselves, but a base type does.
            foreach (var iFace in symbol.AllInterfaces)
            {
                // All classes that derive from IDrawable need to use the source generator.
                // This is conservative for all other (i.e. non-Drawable) classes to avoid polluting irrelevant classes.
                if (SyntaxHelpers.IsIDrawableInterface(iFace) || SyntaxHelpers.IsITransformableInterface(iFace) || SyntaxHelpers.IsISourceGeneratedDependencyActivatorInterface(iFace))
                {
                    addCandidate(context, classSyntax);
                    break;
                }
            }

            // Process any [Cached] attributes on any interface on the class excluding base types.
            foreach (var iFace in symbol.Interfaces)
            {
                // Add an interface entry for all interfaces that have a cached attribute.
                if (iFace.GetAttributes().Any(attrib => SyntaxHelpers.IsCachedAttribute(attrib.AttributeClass)))
                    addCandidate(context, classSyntax).CachedInterfaces.Add(iFace);
            }

            // Process any [Cached] attributes on the class.
            foreach (var attrib in enumerateAttributes(context.SemanticModel, classSyntax))
            {
                if (SyntaxHelpers.IsCachedAttribute(context.SemanticModel, attrib))
                    addCandidate(context, classSyntax).CachedClasses.Add(new SyntaxWithSymbol(context, classSyntax));
            }

            // Process any attributes of members of the class.
            foreach (var member in classSyntax.Members)
            {
                foreach (var attrib in enumerateAttributes(context.SemanticModel, member))
                {
                    if (SyntaxHelpers.IsBackgroundDependencyLoaderAttribute(context.SemanticModel, attrib))
                        addCandidate(context, classSyntax).DependencyLoaderMemebers.Add(new SyntaxWithSymbol(context, member));

                    if (member is not PropertyDeclarationSyntax && member is not FieldDeclarationSyntax)
                        continue;

                    if (SyntaxHelpers.IsResolvedAttribute(context.SemanticModel, attrib))
                        addCandidate(context, classSyntax).ResolvedMembers.Add(new SyntaxWithSymbol(context, member));

                    if (SyntaxHelpers.IsCachedAttribute(context.SemanticModel, attrib))
                        addCandidate(context, classSyntax).CachedMembers.Add(new SyntaxWithSymbol(context, member));
                }
            }
        }

        private static IEnumerable<AttributeSyntax> enumerateAttributes(SemanticModel semanticModel, MemberDeclarationSyntax member)
        {
            return member.AttributeLists
                         .SelectMany(attribList =>
                             attribList.Attributes
                                       .Where(attrib =>
                                           SyntaxHelpers.IsBackgroundDependencyLoaderAttribute(semanticModel, attrib)
                                           || SyntaxHelpers.IsResolvedAttribute(semanticModel, attrib)
                                           || SyntaxHelpers.IsCachedAttribute(semanticModel, attrib)));
        }

        private GeneratorClassCandidate addCandidate(GeneratorSyntaxContext context, ClassDeclarationSyntax classSyntax)
        {
            ITypeSymbol classSymbol = context.SemanticModel.GetDeclaredSymbol(classSyntax)!;

            if (CandidateClasses.TryGetValue(classSymbol, out GeneratorClassCandidate existing))
                return existing;

            return CandidateClasses[classSymbol] = new GeneratorClassCandidate(classSyntax, classSymbol);
        }
    }

    public class GeneratorClassCandidate
    {
        public readonly ClassDeclarationSyntax ClassSyntax;
        public readonly ITypeSymbol Symbol;
        public readonly HashSet<SyntaxWithSymbol> ResolvedMembers = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<SyntaxWithSymbol> CachedMembers = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<SyntaxWithSymbol> CachedClasses = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<SyntaxWithSymbol> DependencyLoaderMemebers = new HashSet<SyntaxWithSymbol>();
        public readonly HashSet<ITypeSymbol> CachedInterfaces = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        public GeneratorClassCandidate(ClassDeclarationSyntax classSyntax, ITypeSymbol symbol)
        {
            ClassSyntax = classSyntax;
            Symbol = symbol;
        }
    }

    public class SyntaxWithSymbol : IEquatable<SyntaxWithSymbol>
    {
        public readonly MemberDeclarationSyntax Syntax;
        public readonly ISymbol Symbol;

        public SyntaxWithSymbol(GeneratorSyntaxContext context, MemberDeclarationSyntax syntax)
        {
            Syntax = syntax;

            if (syntax is FieldDeclarationSyntax field)
                Symbol = context.SemanticModel.GetDeclaredSymbol(field.Declaration.Variables.Single())!;
            else
                Symbol = context.SemanticModel.GetDeclaredSymbol(syntax)!;
        }

        public bool Equals(SyntaxWithSymbol? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return Syntax == other.Syntax;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((SyntaxWithSymbol)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Syntax.GetHashCode() * 397;
            }
        }
    }
}
