// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform;

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
        [Resolved]
        private GameHost host { get; set; }

        public PlatformActionContainer()
            : base(SimultaneousBindingMode.None, KeyCombinationMatchingMode.Modifiers)
        {
        }

        public override IEnumerable<IKeyBinding> DefaultKeyBindings => host.PlatformKeyBindings;

        protected override bool Prioritised => true;
    }
}
