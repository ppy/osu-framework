// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using osu.Framework.SourceGeneration.Emitters;

namespace osu.Framework.SourceGeneration
{
    public static class SyntaxHelpers
    {
        public static InvocationExpressionSyntax CacheDependencyInvocation(string callerType, ExpressionSyntax objSyntax, string? asType, string? cachedName, string? propertyName)
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
                                        SyntaxFactory.ParseTypeName("global::osu.Framework.Utils.SourceGeneratorUtils"),
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

        public static InvocationExpressionSyntax GetDependencyInvocation(string callerType, string requestedType, string? name, string? parent, bool canBeNull, bool rebindBindables)
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
                                        SyntaxFactory.ParseTypeName("global::osu.Framework.Utils.SourceGeneratorUtils"),
                                        SyntaxFactory.GenericName("GetDependency")
                                                     .WithTypeArgumentList(
                                                         SyntaxFactory.TypeArgumentList(
                                                             SyntaxFactory.SeparatedList(new[]
                                                             {
                                                                 SyntaxFactory.ParseTypeName(requestedType)
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

        public static TypeOfExpressionSyntax TypeOf(string typeName)
            => SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(typeName));

        public static bool IsBackgroundDependencyLoaderAttribute(AttributeData? attribute)
            => IsBackgroundDependencyLoaderAttribute(attribute?.AttributeClass);

        public static bool IsResolvedAttribute(AttributeData? attribute)
            => IsResolvedAttribute(attribute?.AttributeClass);

        public static bool IsCachedAttribute(AttributeData? attribute)
            => IsCachedAttribute(attribute?.AttributeClass);

        public static bool IsBackgroundDependencyLoaderAttribute(ITypeSymbol? type)
            => type?.Name == "BackgroundDependencyLoaderAttribute";

        public static bool IsResolvedAttribute(ITypeSymbol? type)
            => type?.Name == "ResolvedAttribute";

        public static bool IsCachedAttribute(ITypeSymbol? type)
            => type?.Name == "CachedAttribute";

        public static bool IsIDependencyInjectionCandidateInterface(ITypeSymbol? type)
            => type?.Name == "IDependencyInjectionCandidate";

        public static string GetFullyQualifiedTypeName(INamedTypeSymbol type)
            => type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

        /// <summary>
        /// Returns a fully-qualified name for the supplied <paramref name="type"/>, with the <c>global::</c> prefix included.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Type keywords are not changed by this method, i.e. an <c>object</c> type symbol will be formatted to string as <c>object</c> rather than <c>System.Object</c>.
        /// </para>
        /// <para>
        /// This method does not return <see langword="null"/> if <paramref name="type"/> is not <see langword="null"/>.
        /// This cannot be expressed via <c>NotNullWhenNotNullAttribute</c> due to targeting .NET Standard 2.0.
        /// </para>
        /// </remarks>
        public static string? GetGlobalPrefixedTypeName(ITypeSymbol? type)
            => type?.ToDisplayString(fullyQualifiedFormatWithNullableAnnotations);

        private static SymbolDisplayFormat fullyQualifiedFormatWithNullableAnnotations { get; } =
            SymbolDisplayFormat.FullyQualifiedFormat
                               .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public static string GetFullyQualifiedSyntaxName(TypeDeclarationSyntax syntax)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var node in syntax.AncestorsAndSelf())
            {
                switch (node)
                {
                    case NamespaceDeclarationSyntax ns:
                        sb.Append(ns.Name);
                        break;

                    case ClassDeclarationSyntax cls:
                        sb.Append(cls.Identifier.ToString());

                        if (cls.TypeParameterList != null)
                            sb.Append($"{{{string.Join(",", cls.TypeParameterList.Parameters.Select(p => p.Identifier.ToString()))}}}");
                        break;

                    default:
                        continue;
                }
            }

            return sb.ToString();
        }

        public static IEnumerable<ITypeSymbol> GetDeclaredInterfacesOnType(INamedTypeSymbol type)
        {
            foreach (var declared in type.Interfaces)
            {
                yield return declared;

                foreach (var nested in declared.AllInterfaces)
                    yield return nested;
            }
        }
    }
}
