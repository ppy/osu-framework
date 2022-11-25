// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    public static class SyntaxHelpers
    {
        public static InvocationExpressionSyntax CacheDependencyInvocation(ITypeSymbol callerType, ExpressionSyntax objSyntax, string? asType, string? cachedName, string? propertyName)
        {
            ExpressionSyntax asTypeSyntax = asType == null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                : SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(asType));

            LiteralExpressionSyntax cachedNameSyntax = cachedName == null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(cachedName));

            LiteralExpressionSyntax memberNameSyntax = propertyName == null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(propertyName));

            return SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ParseTypeName("osu.Framework.Utils.SourceGeneratorUtils"),
                                        SyntaxFactory.IdentifierName("CacheDependency")))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(DependenciesFileEmitter.LOCAL_DEPENDENCIES_VAR_NAME)),
                                            SyntaxFactory.Argument(TypeOf(callerType)),
                                            SyntaxFactory.Argument(objSyntax),
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(DependenciesFileEmitter.CACHE_INFO_PARAMETER_NAME)),
                                            SyntaxFactory.Argument(asTypeSyntax),
                                            SyntaxFactory.Argument(cachedNameSyntax),
                                            SyntaxFactory.Argument(memberNameSyntax)
                                        })));
        }

        public static InvocationExpressionSyntax GetDependencyInvocation(ITypeSymbol callerType, ITypeSymbol requestedType, string? name, string? parent, bool canBeNull, bool rebindBindables)
        {
            LiteralExpressionSyntax nameSyntax = name == null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(name));

            ExpressionSyntax parentSyntax = parent == null
                ? SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)
                : SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(parent));

            LiteralExpressionSyntax canBeNullSyntax = canBeNull
                ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

            LiteralExpressionSyntax rebindBindablesSyntax = rebindBindables
                ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
                : SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);

            return SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.ParseTypeName("osu.Framework.Utils.SourceGeneratorUtils"),
                                        SyntaxFactory.GenericName("GetDependency")
                                                     .WithTypeArgumentList(
                                                         SyntaxFactory.TypeArgumentList(
                                                             SyntaxFactory.SeparatedList(new[]
                                                             {
                                                                 SyntaxFactory.ParseTypeName(GetUnderlyingType(requestedType))
                                                             })))))
                                .WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(new[]
                                        {
                                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(DependenciesFileEmitter.DEPENDENCIES_PARAMETER_NAME)),
                                            SyntaxFactory.Argument(TypeOf(callerType)),
                                            SyntaxFactory.Argument(nameSyntax),
                                            SyntaxFactory.Argument(parentSyntax),
                                            SyntaxFactory.Argument(canBeNullSyntax),
                                            SyntaxFactory.Argument(rebindBindablesSyntax)
                                        })));
        }

        public static string GetUnderlyingType(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            {
                if (typeSymbol.IsValueType)
                {
                    // For value types, the "original definition" / underlying type is System.Nullable<T>,
                    // and the correct type is present as the first of the symbol's type parameters.
                    typeSymbol = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
                }
                else
                {
                    // For reference types... I have no idea how to retrieve the underlying type.
                    // "OriginalDefinition" fails for generics (e.g. "Bindable<int>"? becomes "Bindable<T>").
                    // So... We'll just do manual string manipulation for now.
                    // Todo: Maybe use this as a general path?
                    return typeSymbol.ToDisplayString().TrimEnd('?');
                }
            }

            return typeSymbol.ToDisplayString();
        }

        public static TypeOfExpressionSyntax TypeOf(ITypeSymbol typeSymbol)
            => SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(GetUnderlyingType(typeSymbol)));

        public static bool IsBackgroundDependencyLoaderAttribute(SemanticModel semanticModel, AttributeSyntax attribute)
            => IsBackgroundDependencyLoaderAttribute(semanticModel.GetTypeInfo(attribute.Name).Type);

        public static bool IsResolvedAttribute(SemanticModel semanticModel, AttributeSyntax attribute)
            => IsResolvedAttribute(semanticModel.GetTypeInfo(attribute.Name).Type);

        public static bool IsCachedAttribute(SemanticModel semanticModel, AttributeSyntax attribute)
            => IsCachedAttribute(semanticModel.GetTypeInfo(attribute.Name).Type);

        public static bool IsBackgroundDependencyLoaderAttribute(ITypeSymbol? type)
            => type?.Name == "BackgroundDependencyLoaderAttribute";

        public static bool IsResolvedAttribute(ITypeSymbol? type)
            => type?.Name == "ResolvedAttribute";

        public static bool IsCachedAttribute(ITypeSymbol? type)
            => type?.Name == "CachedAttribute";

        public static bool IsIDrawableInterface(ITypeSymbol? type)
            => type?.Name == "IDrawable";

        public static bool IsITransformableInterface(ITypeSymbol? type)
            => type?.Name == "ITransformable";

        public static bool IsISourceGeneratedDependencyActivatorInterface(ITypeSymbol? type)
            => type?.Name == "ISourceGeneratedDependencyActivator";

        public static bool IsIReadOnlyDependencyContainerInterface(ITypeSymbol? type)
            => type?.Name == "IReadOnlyDependencyContainer";

        public static bool IsTransformableType(ITypeSymbol? type)
            => type?.Name == "Transformable";

        public static IEnumerable<ITypeSymbol> EnumerateBaseTypes(ITypeSymbol type)
        {
            INamedTypeSymbol? baseType = type.BaseType;

            while (baseType != null)
            {
                yield return baseType;

                baseType = baseType.BaseType;
            }
        }

        public static IEnumerable<AttributeData> EnumerateDependencyInjectionAttributes(ISymbol symbol)
        {
            return symbol.GetAttributes()
                         .Where(attrib =>
                             IsBackgroundDependencyLoaderAttribute(attrib.AttributeClass)
                             || IsResolvedAttribute(attrib.AttributeClass)
                             || IsCachedAttribute(attrib.AttributeClass));
        }

        public static IEnumerable<AttributeSyntax> EnumerateDependencyInjectionAttributes(SemanticModel semanticModel, MemberDeclarationSyntax member)
        {
            return member.AttributeLists
                         .SelectMany(l => l.Attributes)
                         .Where(attrib =>
                             IsBackgroundDependencyLoaderAttribute(semanticModel, attrib)
                             || IsResolvedAttribute(semanticModel, attrib)
                             || IsCachedAttribute(semanticModel, attrib));
        }
    }
}
