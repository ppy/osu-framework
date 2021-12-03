// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Input
{
    /// <summary>
    /// Provides actions that are expected to have different key bindings per platform.
    /// The framework will always contain one top-level instance of this class, but extra instances
    /// can be created to handle events that should trigger specifically on a focused drawable.
    /// Will send repeat events by default.
    /// </summary>
    public class PlatformActionContainer : KeyBindingContainer<PlatformAction>, IHandleGlobalKeyboardInput
    {
        public PlatformActionContainer()
            : base(SimultaneousBindingMode.None, KeyCombinationMatchingMode.Modifiers)
        {
        }

        public override IEnumerable<IKeyBinding> DefaultKeyBindings => Host.PlatformKeyBindings;

        protected override bool Prioritised => true;
    }
}
