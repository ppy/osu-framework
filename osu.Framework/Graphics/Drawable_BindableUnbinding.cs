// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Logging;

namespace osu.Framework.Graphics
{
    public partial class Drawable
    {
        private void unbindAllBindables()
        {
            if (this is ISourceGeneratedUnbindAllBindables sg && sg.KnownType == GetType())
            {
                sg.InternalUnbindAllBindables();
                return;
            }

            reflectUnbindAction().Invoke(this);
        }

        private Action<object> reflectUnbindAction()
        {
            Type ourType = GetType();
            return unbind_action_cache.TryGetValue(ourType, out var action) ? action : addToCache(ourType);

            // Extracted to a separate method to prevent .NET from pre-allocating some objects (saves ~150B per call to this method, even if already cached).
            static Action<object> addToCache(Type ourType)
            {
                List<Action<object>> actions = new List<Action<object>>();

                foreach (var type in ourType.EnumerateBaseTypes())
                {
                    // Generate delegates to unbind fields
                    actions.AddRange(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                         .Where(f => typeof(IUnbindable).IsAssignableFrom(f.FieldType))
                                         .Select(f => new Action<object>(target => ((IUnbindable?)f.GetValue(target))?.UnbindAll())));
                }

                // Delegates to unbind properties are intentionally not generated.
                // Properties with backing fields (including automatic properties) will be picked up by the field unbind delegate generation,
                // while ones without backing fields (like get-only properties that delegate to another drawable's bindable) should not be unbound here.

                return unbind_action_cache[ourType] = target =>
                {
                    foreach (var a in actions)
                    {
                        try
                        {
                            a(target);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"Failed to unbind a local bindable in {ourType.ReadableName()}");
                        }
                    }
                };
            }
        }
    }
}
