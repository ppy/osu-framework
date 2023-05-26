// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace osu.Framework.SourceGeneration.Generators.HandleInput
{
    public class HandleInputSemanticTarget : IncrementalSemanticTarget
    {
        public bool RequestsPositionalInput { get; private set; }
        public bool RequestsNonPositionalInput { get; private set; }

        public HandleInputSemanticTarget(ClassDeclarationSyntax classSyntax, SemanticModel semanticModel)
            : base(classSyntax, semanticModel)
        {
        }

        protected override bool CheckValid(INamedTypeSymbol symbol)
        {
            INamedTypeSymbol? s = symbol;

            while (s != null)
            {
                if (isDrawableType(s))
                    return true;

                s = s.BaseType;
            }

            return false;
        }

        // This source generator never overrides.
        protected override bool CheckNeedsOverride(INamedTypeSymbol symbol) => false;

        protected override void Process(INamedTypeSymbol symbol)
        {
            if (FullyQualifiedTypeName == "osu.Framework.Graphics.Drawable")
                return;

            RequestsPositionalInput =
                checkMethods(positional_input_methods, symbol)
                || checkInterfaces(positional_input_interfaces, symbol)
                || checkProperties(positional_input_properties, symbol);

            RequestsNonPositionalInput =
                checkMethods(non_positional_input_methods, symbol)
                || checkInterfaces(non_positional_input_interfaces, symbol)
                || checkProperties(non_positional_input_properties, symbol);
        }

        private static bool checkMethods(IEnumerable<string> methods, INamedTypeSymbol symbol)
        {
            return runForTypeHierarchy(symbol, methods, static (symbol, methods) =>
            {
                return methods.SelectMany(name => symbol.GetMembers(name).OfType<IMethodSymbol>()).Any(isDrawableMethod);
            });
        }

        private static bool checkProperties(IEnumerable<string> properties, INamedTypeSymbol symbol)
        {
            return runForTypeHierarchy(symbol, properties, static (symbol, properties) =>
            {
                return properties.SelectMany(name => symbol.GetMembers(name).OfType<IPropertySymbol>()).Any(isDrawableProperty);
            });
        }

        private static bool checkInterfaces(ImmutableHashSet<string> interfaces, INamedTypeSymbol symbol)
        {
            return symbol.AllInterfaces.Select(SyntaxHelpers.GetFullyQualifiedTypeName).Any(interfaces.Contains);
        }

        private static bool runForTypeHierarchy<T>(INamedTypeSymbol symbol, T items, Func<INamedTypeSymbol, T, bool> func)
        {
            INamedTypeSymbol? type = symbol;

            while (type != null && !isDrawableType(type))
            {
                if (func(type, items))
                    return true;

                type = type.BaseType;
            }

            return false;
        }

        private static bool isDrawableMethod(IMethodSymbol method)
        {
            while (method.OverriddenMethod != null)
                method = method.OverriddenMethod;

            return isDrawableType(method.ContainingType);
        }

        private static bool isDrawableProperty(IPropertySymbol property)
        {
            while (property.OverriddenProperty != null)
                property = property.OverriddenProperty;

            return isDrawableType(property.ContainingType);
        }

        private static bool isDrawableType(INamedTypeSymbol type)
            => SyntaxHelpers.GetFullyQualifiedTypeName(type) == "osu.Framework.Graphics.Drawable";

        // HandleInputCache.positional_input_methods
        private static readonly string[] positional_input_methods =
        {
            "Handle",
            "OnMouseMove",
            "OnHover",
            "OnHoverLost",
            "OnMouseDown",
            "OnMouseUp",
            "OnClick",
            "OnDoubleClick",
            "OnDragStart",
            "OnDrag",
            "OnDragEnd",
            "OnScroll",
            "OnFocus",
            "OnFocusLost",
            "OnTouchDown",
            "OnTouchMove",
            "OnTouchUp",
            "OnTabletPenButtonPress",
            "OnTabletPenButtonRelease"
        };

        // HandleInputCache.non_positional_input_methods
        private static readonly string[] non_positional_input_methods =
        {
            "Handle",
            "OnFocus",
            "OnFocusLost",
            "OnKeyDown",
            "OnKeyUp",
            "OnJoystickPress",
            "OnJoystickRelease",
            "OnJoystickAxisMove",
            "OnTabletAuxiliaryButtonPress",
            "OnTabletAuxiliaryButtonRelease",
            "OnMidiDown",
            "OnMidiUp"
        };

        // HandleInputCache.positional_input_interfaces
        private static readonly ImmutableHashSet<string> positional_input_interfaces = ImmutableHashSet.Create<string>(
            "osu.Framework.Graphics.Cursor.IHasTooltip",
            "osu.Framework.Graphics.Cursor.IHasCustomTooltip",
            "osu.Framework.Graphics.Cursor.IHasContextMenu",
            "osu.Framework.Graphics.Cursor.IHasPopover"
        );

        // HandleInputCache.non_positional_input_interfaces
        private static readonly ImmutableHashSet<string> non_positional_input_interfaces = ImmutableHashSet.Create<string>(
            "osu.Framework.Input.Bindings.IKeyBindingHandler"
        );

        // HandleInputCache.positional_input_properties
        private static readonly string[] positional_input_properties =
        {
            "HandlePositionalInput"
        };

        // HandleInputCache.non_positional_input_properties
        private static readonly string[] non_positional_input_properties =
        {
            "HandleNonPositionalInput",
            "AcceptsFocus"
        };
    }
}
