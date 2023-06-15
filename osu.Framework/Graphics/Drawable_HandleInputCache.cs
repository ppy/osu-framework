// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Graphics
{
    public partial class Drawable
    {
        /// <summary>
        /// Nested class which is used for caching <see cref="HandleNonPositionalInput"/>, <see cref="HandlePositionalInput"/> values.
        /// </summary>
        internal static class HandleInputCache
        {
            private static readonly ConcurrentDictionary<Type, bool> positional_cached_values = new ConcurrentDictionary<Type, bool>();
            private static readonly ConcurrentDictionary<Type, bool> non_positional_cached_values = new ConcurrentDictionary<Type, bool>();

            private static readonly string[] positional_input_methods =
            {
                nameof(Handle),
                nameof(OnMouseMove),
                nameof(OnHover),
                nameof(OnHoverLost),
                nameof(OnMouseDown),
                nameof(OnMouseUp),
                nameof(OnClick),
                nameof(OnDoubleClick),
                nameof(OnDragStart),
                nameof(OnDrag),
                nameof(OnDragEnd),
                nameof(OnScroll),
                nameof(OnFocus),
                nameof(OnFocusLost),
                nameof(OnTouchDown),
                nameof(OnTouchMove),
                nameof(OnTouchUp),
                nameof(OnTabletPenButtonPress),
                nameof(OnTabletPenButtonRelease)
            };

            private static readonly string[] non_positional_input_methods =
            {
                nameof(Handle),
                nameof(OnFocus),
                nameof(OnFocusLost),
                nameof(OnKeyDown),
                nameof(OnKeyUp),
                nameof(OnJoystickPress),
                nameof(OnJoystickRelease),
                nameof(OnJoystickAxisMove),
                nameof(OnTabletAuxiliaryButtonPress),
                nameof(OnTabletAuxiliaryButtonRelease),
                nameof(OnMidiDown),
                nameof(OnMidiUp)
            };

            private static readonly Type[] positional_input_interfaces =
            {
                typeof(IHasTooltip),
                typeof(IHasCustomTooltip),
                typeof(IHasContextMenu),
                typeof(IHasPopover),
            };

            private static readonly Type[] non_positional_input_interfaces =
            {
                typeof(IKeyBindingHandler),
            };

            private static readonly string[] positional_input_properties =
            {
                nameof(HandlePositionalInput),
            };

            private static readonly string[] non_positional_input_properties =
            {
                nameof(HandleNonPositionalInput),
                nameof(AcceptsFocus),
            };

            public static bool RequestsPositionalInput(Drawable drawable)
            {
                if (drawable is ISourceGeneratedHandleInputCache sgInput && sgInput.KnownType == drawable.GetType())
                    return sgInput.RequestsPositionalInput;

                return getViaReflection(drawable, positional_cached_values, true);
            }

            public static bool RequestsNonPositionalInput(Drawable drawable)
            {
                if (drawable is ISourceGeneratedHandleInputCache sgInput && sgInput.KnownType == drawable.GetType())
                    return sgInput.RequestsNonPositionalInput;

                return getViaReflection(drawable, non_positional_cached_values, false);
            }

            private static bool getViaReflection(Drawable drawable, ConcurrentDictionary<Type, bool> cache, bool positional)
            {
                var type = drawable.GetType();

                if (!cache.TryGetValue(type, out bool value))
                {
                    value = computeViaReflection(type, positional);
                    cache.TryAdd(type, value);
                }

                return value;
            }

            private static bool computeViaReflection(Type type, bool positional)
            {
                string[] inputMethods = positional ? positional_input_methods : non_positional_input_methods;

                foreach (string inputMethod in inputMethods)
                {
                    // check for any input method overrides which are at a higher level than drawable.
                    var method = type.GetMethod(inputMethod, BindingFlags.Instance | BindingFlags.NonPublic);

                    Debug.Assert(method != null);

                    if (method.DeclaringType != typeof(Drawable))
                        return true;
                }

                var inputInterfaces = positional ? positional_input_interfaces : non_positional_input_interfaces;

                foreach (var inputInterface in inputInterfaces)
                {
                    // check if this type implements any interface which requires a drawable to handle input.
                    if (inputInterface.IsAssignableFrom(type))
                        return true;
                }

                string[] inputProperties = positional ? positional_input_properties : non_positional_input_properties;

                foreach (string inputProperty in inputProperties)
                {
                    var property = type.GetProperty(inputProperty);

                    Debug.Assert(property != null);

                    if (property.DeclaringType != typeof(Drawable))
                        return true;
                }

                return false;
            }
        }
    }
}
